using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Diagnostics;

namespace ClassLibrary1
{
    public class crawler
    {
        //Contains the disallowed values
        private List<String> disallowedList;
        public crawler()
        {
            disallowedList = new List<string>();
        }

        //Go through the 2 sitemaps and grab the xml files in the initual screening. Also add the disallowed values into the list
        public List<String> parseXml()
        {
            var wc = new WebClient();
            List<String> allowedList = new List<String>();
            String[] robotSites = new String[2];
            robotSites[0] = "http://bleacherreport.com/robots.txt";
            robotSites[1] = "http://www.cnn.com/robots.txt";
            for (int i = 0; i < robotSites.Length; i++)
            {
                String robot = robotSites[i];
                using (var sourceStream = wc.OpenRead(robot))
                {
                    using (var reader = new StreamReader(sourceStream))
                    {
                        while (reader.EndOfStream == false)
                        {
                            string line = reader.ReadLine();
                            if (!line.Contains("User-Agent"))
                            {
                                if (line.Contains("Sitemap:"))
                                {
                                    String newUrl = line.Replace("Sitemap:", "").Trim();

                                    if (robot.Contains("bleacher") && newUrl.Contains("nba"))
                                    {
                                        allowedList.Add(newUrl);
                                    }
                                    else if (robot.Contains("cnn"))
                                    {
                                        allowedList.Add(newUrl);
                                    }
                                }
                                if (line.Contains("Disallow:"))
                                {
                                    String newUrl = line.Replace("Disallow:", "").Trim();
                                    if (robot.Contains("bleacher"))
                                    {
                                        newUrl = "http://bleacherreport.com" + newUrl;
                                    }
                                    else
                                    {
                                        newUrl = "http://cnn.com" + newUrl;
                                    }
                                    disallowedList.Add(newUrl);
                                }
                            }
                        }
                    }
                }
            }
           return allowedList;
        }

        //Get the url and the root and append the two
        public string refineUrl(string url, string root)
        {
            if (url.StartsWith("//"))
            {
                url = "http:" + url;
            }
            else if (url.StartsWith("/"))
            {
                url = "http://" + root + url;
            }
            return url;
        }

        //Check if the url is allowed to be crawleds
        public Boolean isAllowed(string url)
        {
            Boolean isAllowed = true;
            for (int i = 0; i < disallowedList.Count; i++)
            {
                String disallowedUrl = disallowedList[i];
                if (url.Contains(disallowedUrl))
                {
                    isAllowed = false;
                    break;
                }
            }
            return isAllowed;
        }

        //Get the title from the webpage
        public string getTitle(HtmlNode node)
        {
            string title = "";   
            if (node != null)
            {
                title = node.InnerHtml;
            }
            
            return title;
        }

        public List<String> keyTitles(String title)
        {
            List<String> t = new List<String>();
            title = title.ToLower();
           String newTitle = Regex.Replace(title, @"[^\w\.@\ ]", "");
            string[] words = newTitle.Split(' ');
            foreach (string word in words)
            {
                t.Add(word);
            }
            return t;
        }

        //Get the root from the webpage
        public string getRoot(String retrievedMessage)
        {
            String root = "";

            if (retrievedMessage.Contains("bleacher"))
            {
                root = "bleacherreport.com";
            }
            else if (retrievedMessage.Contains("cnn"))
            {
                root = "cnn.com";
            }
            return root;
        }

        //Get the date from the webpage
        public string getDate(HtmlNode node)
        {
            String date = "";
            if (node != null)
            {
                date = node.GetAttributeValue("content", "");
            }
            return date;
        }


    }
}
