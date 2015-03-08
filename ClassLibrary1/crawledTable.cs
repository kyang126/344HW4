using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;


namespace ClassLibrary1
{
    public class crawledTable : TableEntity
    {
        //Takes in the url, title, date, error, lastten, tableSize, ram, cpu, status of worker to display on the dashboard
        public crawledTable(string value, string url, string title, string date, string error, string lastten, string rowkey, int tableSize, 
            string ram, string cpu, string status, string titleNumber, string lastTitle)
        {
            this.PartitionKey = value;
            this.RowKey = rowkey;
            this.url = url;
            this.title = title;
            this.date = date;
            this.lastten = lastten;
            this.error = error;
            this.tableSize = tableSize;
            this.ram = ram;
            this.cpu = cpu;
            this.status = status;
            this.titleNumber = titleNumber;
            this.lastTitle = lastTitle;
        }

        public crawledTable() { }

        public int tableSize { get; set; }

        public string status { get; set; }

        public string titleNumber { get; set; }

        public string lastTitle { get; set; }

        public string ram { get; set; }

        public string url { get; set; }

        public string cpu { get; set; }
        public string error { get; set; }

        public string title { get; set; }

        public string date { get; set; }

        public string lastten { get; set; }



    }
}
