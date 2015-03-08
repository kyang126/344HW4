using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for QuerySuggest
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class QuerySuggest : System.Web.Services.WebService
    {

        private static BuildTrie bt;
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");

        //Gets the standardized text file from the blob storage and downloads it to a temporary directory
        [WebMethod]
        public void getData()
        {
            //References the text file in the blob storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("344storage");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("hwData.txt");

            //Creates a temporary path for the file to be downloaded to
            var filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";
            using (var fileStream = System.IO.File.OpenWrite(filePath))
            {
                blockBlob.DownloadToStream(fileStream);
            }
        }

        //Uses the temporary file created from the previous method to populate the trie
        [WebMethod]
        public String buildTrie()
        {
            var filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";
            bt = new BuildTrie();
            int titleCounter = 0;
            int counter = 0;
            getReference g = new getReference();
                CloudTable table = g.getTable();
                float memory = this.theMemCounter.NextValue();
                string line = "";
            //Reads the file created from before to populate the trie until there is less than 20MB left
            using (StreamReader sr = new StreamReader(filePath))
            { 
                string lastTitle = "";
                while (sr.EndOfStream == false)
                {
                    //The memory left at the cloud on Azure
                    if (counter/1000 != 0)
                    {
                        memory = this.theMemCounter.NextValue();
                        counter = 0;
                    }
                    line = sr.ReadLine();
                    titleCounter++;
                    counter++;
                    if (memory > 50)
                    {
                        try
                        {
                            bt.addWord(line.ToLower());
                        }
                        catch
                        {

                        }
                        
                    }
                    else
                    {
                       
                        break;
                    }
                }
                crawledTable dashboard = new crawledTable("query", null, null, null, null, null, "querykey", 0, null, null, "idle", "" +titleCounter,line);
                TableOperation insertOrReplaceOperation1 = TableOperation.InsertOrReplace(dashboard);
                table.Execute(insertOrReplaceOperation1);
                return lastTitle;
            }
        }

        //Takes the search text from an input field and outputs up to 10 words that closely matches that term
        [WebMethod]
        public List<String> getSuggestions(String term)
        {
            //Returns list of words that closely matches the search term
            return bt.search(term);
        }
    }
}
