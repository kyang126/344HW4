﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using ClassLibrary1;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebRole1
{
    /// <summary>
    /// Summary description for dashboard
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class dashboard : System.Web.Services.WebService
    {
    private static Dictionary<String, List<String>> localCache = new
    Dictionary<String, List<String>>();


        //Puts the word start to the CloudQueue for the worker to see
        [WebMethod]
        public void startCrawl()
        {
            getReference g = new getReference();
            CloudQueue queue = g.commandQueue();
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("start");
            queue.AddMessage(message);
        }

        //Puts the word stop for the worker to see
        [WebMethod]
        public void stopCrawl()
        {
            getReference g = new getReference();
            CloudQueue queue = g.commandQueue();
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage("stop");
            queue.AddMessage(message);
        }

        //Gets the last ten urls crawled
        [WebMethod]
        public List<String> lastTen()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            List<String> t = new List<String>();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            String value = ((crawledTable)retrievedResult.Result).lastten;
            if (!String.IsNullOrEmpty(value) && value.Contains(','))
            {
                string[] values = value.Split(',');
                for (int i = 0; i < values.Length; i++)
                {
                    t.Add(values[i]);
                }
            }
                return t;
        }

        //Gets the last ten errors
        [WebMethod]
        public List<String> errors()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            List<String> t = new List<String>();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            String value = ((crawledTable)retrievedResult.Result).error;
            if (!String.IsNullOrEmpty(value) && value.Contains(','))
            {
                string[] values = value.Split(',');
                for (int i = 0; i < values.Length; i++)
                {
                    t.Add(values[i]);
                }
            }
            return t;
        }

        //Gets the index size
        [WebMethod]
        public int tableSize()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            int value = ((crawledTable)retrievedResult.Result).tableSize;
            return value;
        }

        //Gets the index size
        [WebMethod]
        public int urlSize()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            int value = ((crawledTable)retrievedResult.Result).urlSize;
            return value;
        }

        //Gets the ram size
        [WebMethod]
        public string ramSize()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            string value = ((crawledTable)retrievedResult.Result).ram;
            return value;
        }

        //Grabbed the number of titles inserted into the trie
        [WebMethod]
        public string titleSize()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("query", "querykey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            string value = ((crawledTable)retrievedResult.Result).titleNumber;
            return value;
        }

        //Grabbing the last title inserted into the trie
        [WebMethod]
        public string lastTitle()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("query", "querykey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            string value = ((crawledTable)retrievedResult.Result).lastTitle;
            return value;
        }

        //Gets the cpu size
        [WebMethod]
        public string cpuSize()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            string value = ((crawledTable)retrievedResult.Result).cpu;
            return value;
        }

        //Clears the Queue
        [WebMethod]
        public void clearQ()
        {
            getReference g = new getReference();
            CloudQueue queue = g.getQueue();

            queue.Clear();
        }

        //Clears the index
        [WebMethod]
        public void deleteTable()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            table.Delete();
        }

        //Gets the size of the queue
        [WebMethod]
        public String getQueueCount()
        {
            getReference g = new getReference();
            CloudQueue queue = g.getQueue();
            queue.CreateIfNotExists();
            queue.FetchAttributes();
            int approximateMessagesCount = queue.ApproximateMessageCount.Value;
            return "" + approximateMessagesCount;
        }

        //Gets the status of the worker
        [WebMethod]
        public String getStatus()
        {
            getReference g = new getReference();
            CloudTable table = g.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<crawledTable>("dash", "rowkey");
            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            string value = ((crawledTable)retrievedResult.Result).status;
            return value;
        }

        //Allows user to clear the cache
        [WebMethod]
        public void clearCache()
        {
            localCache.Clear();
        }

        //Searches the index for the title based on the url inputed
        [WebMethod]
        public List<String> search(String term)
        {
            List<string> suggestions = new List<string>();
            getReference g = new getReference();
            CloudTable table = g.getTable();
            List<crawledTable> t = new List<crawledTable>();
            term = term.ToLower();
            if (!localCache.ContainsKey(term))
            {
                List<String> searchWords = new List<String>();

                string[] words = term.ToLower().Split(' ');
                foreach (string word in words)
                {
                    searchWords.Add(word);
                }

                for (int i = 0; i < searchWords.Count; i++)
                {
                    TableQuery<crawledTable> rangeQuery = new TableQuery<crawledTable>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, searchWords[i]));

                    foreach (crawledTable entity in table.ExecuteQuery(rangeQuery))
                    {
                        t.Add(entity);
                    }
                }
                suggestions = tenSearch(t);

                localCache.Add(term, suggestions);
                return suggestions;
                      
            } else
            {
                List<string> a = localCache[term];
                return a; ;
            }
        }

        //Get the ten results ordered by count and date
        private List<String> tenSearch(List<crawledTable> s)
        {

            var order = s.Select(x=> new Tuple<string, string, string> (x.url, x.title, x.date))
                .GroupBy(x => x.Item1)
                .Select(x => new Tuple<string, string, string, int>(x.Key, x.ToList().First().Item2, x.ToList().First().Item3, x.Count()))
                .OrderByDescending(x=>x.Item4)
                .ThenByDescending((x=>x.Item3))
                .Take(10);

            List<String> tenList = new List<String>();

            foreach (var nameGroup in order)
            {
                tenList.Add("<h3>"+nameGroup.Item2 + "</h3>" + nameGroup.Item1 + "<br/>" + nameGroup.Item3 + "<br/>");
            }
            return tenList;
        }

        //Encodes the url to match the row key
        private static String EncodeUrlInKey(String url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }

    }
}
