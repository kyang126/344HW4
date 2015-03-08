using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Text.RegularExpressions;
using ClassLibrary1;
using System.Text;
using HtmlAgilityPack;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        private PerformanceCounter cpuload = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        /*This method runs on load and will check if the user has pressed the start button. If the user has then the crawler will begin to parse the
         * html pages. Else the dashboard will let the user know that the crawler is idle
         * 
         * */
        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            try
            {
                getReference g = new getReference();
                CloudQueue cmd = g.commandQueue();
                CloudTable table = g.getTable();
                while (true)
                {
                    //Check if the user has submitted the button
                    CloudQueueMessage retrievedMessage = cmd.GetMessage();

                    //Let the user know about the status of the worker
                    crawledTable dashboard = new crawledTable("dash", null, null, null, null, null, "rowkey", 0, null, null, "idle", null, null);
                    TableOperation insertOrReplaceOperation1 = TableOperation.InsertOrReplace(dashboard);
                    table.Execute(insertOrReplaceOperation1);

                    //If the user pressed the button start crawling the pages
                    if (retrievedMessage != null && retrievedMessage.AsString.Equals("start"))
                    {
                        cmd.DeleteMessage(retrievedMessage);
                        crawler crawlWeb = new crawler();
                        List<String> allowedList = crawlWeb.parseXml();
                        HashSet<String> visitedList = getAllHtml(allowedList);             
                        crawlerUrls(visitedList, crawlWeb);
                    }
                    Thread.Sleep(20);
                }
                
               this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        //The list of visited xml files that the worker role has seen will be inputted
        //The output will be a HashSet of the html links that have been seen from the worker in this initial scan
        private HashSet<String> getAllHtml(List<String> visitedList)
        {
            HashSet<String> htmlList = new HashSet<String>();
            int count = 0;
            getReference g = new getReference();
            CloudQueue queue = g.getQueue();
            CloudTable table = g.getTable();

            //Look through the xml files and go deeper into them if there are multiple layers. The list will grow for every layer of xml files to parse through
            while (count < visitedList.Count)
            {

                //Go into the xml page and grab the url/xml file
                WebClient web = new WebClient();
                String html = web.DownloadString(visitedList.ElementAt(count));
                MatchCollection m1 = Regex.Matches(html, @"<loc>\s*(.+?)\s*</loc>", RegexOptions.Singleline);
                
                //If the links are xml it will add it to the 'queue' to look deeper in
                //If links are html it will add it to the CloudQueue and the visited hashset
                foreach (Match m in m1)
                {

                    //The filter checks if it is a hashset, is in the year 2015, and doesn't contain a year in the url
                    String url = m.Groups[1].Value;
                    if (url.Contains("xml") && ((url.Contains("2015") || !url.Contains("-20"))))
                    {
                        visitedList.Add(url);
                    }

                    //The filter checks if it is an html file and adds it to the CloudQueue and visited list. Also lets the user know that it is in the loading phase.
                    if (!url.Contains("xml"))
                    {
                        CloudQueueMessage message = new CloudQueueMessage(url);
                        queue.AddMessageAsync(message);
                        htmlList.Add(url);
                    }
                }
                crawledTable dashboard = new crawledTable("dash", null, null, null, null, null, "rowkey", 0, null, null, "loading", null, null);
                TableOperation insertOrReplaceOperation1 = TableOperation.InsertOrReplace(dashboard);
                table.Execute(insertOrReplaceOperation1);
                count++;
            }
            return htmlList;
        }

        //This method takes in the duplicates list and the crawler class 
        //This method will look through every page in cnn and bleacherreport while adhering to the filters that
        //I set such as duplicate, disallowed list, and etc.
        public void crawlerUrls(HashSet<String> duplicateList, crawler crawlWeb)
        {
            List<String> lastten = new List<String>();
            List<String> errorList = new List<String>();
            getReference g = new getReference();
            CloudQueue queue = g.getQueue();
            CloudTable table = g.getTable();
            CloudQueue cmd = g.commandQueue();
            queue.FetchAttributes();
            var limitCount = queue.ApproximateMessageCount.Value;
            int tableSize = 0;

            //While the CloudQueue that holds the links that have yet to be visited isn't empty, this will parse through every link
            while (0 < limitCount)
            {

                //Grabs the link to be parsed in the queue
                CloudQueueMessage retrievedMessage = queue.GetMessage();
                try
                {
                    if (retrievedMessage != null)
                    {

                        //This goes to the webpage
                        HtmlWeb web = new HtmlWeb();
                        HtmlDocument document = web.Load(retrievedMessage.AsString);

                        //Grabs the title in the page
                        HtmlNode node = document.DocumentNode.SelectSingleNode("//title");
                        String title = crawlWeb.getTitle(node);
                        List<String> keyTitles = crawlWeb.keyTitles(title);

                        //Grabs the date in the page
                        HtmlNode dateNode = document.DocumentNode.SelectSingleNode("//meta[(@itemprop='dateCreated')]");
                        String date = crawlWeb.getDate(dateNode);

                        //This ensures that I keep track of the last ten urls that were added to the table
                        String tenUrls = "";
                        lastten.Add(retrievedMessage.AsString);
                        if (lastten.Count == 11)
                        {
                            lastten.RemoveAt(0);
                            tenUrls = String.Join(",", lastten);
                        }

                        //Delete the url since already visited now
                        queue.DeleteMessage(retrievedMessage);

                        //Encode the url to enable it to be placed as a rowkey
                        String encodeUrl = EncodeUrlInKey(retrievedMessage.AsString);

                        //Get the memory and cpu of the worker role
                        float memory = this.theMemCounter.NextValue();
                        float cpuUtilization = this.cpuload.NextValue();

                        tableSize++;
                        
                       
                        //Converts the error list to a single string divided by commas
                        String errors = String.Join(",", errorList);

                        //Insert the data into the cloudtable
                        insertTable(table, retrievedMessage.AsString, title, date, errors, tenUrls, tableSize, memory.ToString(), cpuUtilization.ToString(), encodeUrl, keyTitles);
                        
                        //get the root of the url that is being parsed
                        string root = crawlWeb.getRoot(retrievedMessage.AsString);

                        //Get the links on the webpage
                        var rows = document.DocumentNode.SelectNodes("//a[@href]");
                        if (rows != null && rows.Count > 0)
                        {

                            //For all the links in the webpage
                            foreach (var link in rows)
                            {
                                String url = link.Attributes["href"].Value;

                                //Standardize the url
                                url = crawlWeb.refineUrl(url, root);

                                //Check if the url is in the disallowed list
                                Boolean isAllowed = crawlWeb.isAllowed(url);

                                //Add to the CloudQueue if it passes the tests
                                if (!duplicateList.Contains(url) && isAllowed && (url.Contains(root + "/")))
                                {
                                    duplicateList.Add(url);
                                    CloudQueueMessage message = new CloudQueueMessage(url);
                                    queue.AddMessageAsync(message);
                                }
                            }
                        }
                    }
                }

                //If there was an error in going to the website tell the user in the dashboard
                catch (Exception e)
                {
                    string errorMessage = "Url: " + retrievedMessage.AsString + " problem is: " + e.Message;
                    if (!errorList.Contains(errorMessage))
                    {
                        errorList.Add(errorMessage);
                        if (errorList.Count == 11)
                        {
                            errorList.RemoveAt(0);   
                        }
                      
                    }

                }
                queue.FetchAttributes();
                limitCount = queue.ApproximateMessageCount.Value;
            }
        }

        //The table takes in the fields CloudTable, url, title, date, errors, tenUrls, tableSize, memory, cpu, encodeUrl
        //Puts in the values into table
        private void insertTable(CloudTable table, string url, string title, string date, string errors, string tenUrls, int tableSize, string memory, string cpuUtilization, string encodeUrl, List<String> keyTitles)
        {
            for (int i = 0; i < keyTitles.Count; i++)
            {
                if (!keyTitles[i].Equals(""))
                {
                    crawledTable ct = new crawledTable(keyTitles[i], url, title, date, null, null, encodeUrl, 0, null, null, null, null, null);
                    TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(ct);
                    table.ExecuteAsync(insertOrReplaceOperation);
                }   
            }           
            crawledTable dashboard = new crawledTable("dash", url, title, date, errors, tenUrls, "rowkey", tableSize, memory, cpuUtilization + "%", "crawling", null, null);
            TableOperation insertOrReplaceOperation1 = TableOperation.InsertOrReplace(dashboard);
       
            table.ExecuteAsync(insertOrReplaceOperation1);

        }

        //Encodes the url
        private static String EncodeUrlInKey(String url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}