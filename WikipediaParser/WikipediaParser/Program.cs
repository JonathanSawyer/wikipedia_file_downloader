using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace WikipediaParser
{
    class Program
    {
        static ManualResetEvent resetEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            string[] topics = ConfigurationManager.AppSettings["topics"].ToLower().Split(',');            

            int numThreads = topics.Count();           

            for (int index = 0; index < numThreads; index++)
            {
                var topic = topics[index];
                if(!String.IsNullOrWhiteSpace(topic))
                    new Thread(() => DownloadContent(topic, ref numThreads)).Start();
            }
            resetEvent.WaitOne();
            Console.WriteLine("Finished.");
            Console.ReadLine();
        }

        private static void DownloadContent(string topic, ref int numThreads) 
        {
            Console.WriteLine("Downloading " + topic + "...");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format(ConfigurationManager.AppSettings["wikipedia_url"], topic));
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
            {
                string jsonResult = responseReader.ReadToEnd();
                var json = JObject.Parse(jsonResult);
                responseReader.Close();

                StringBuilder builder = new StringBuilder();
                foreach (var children in json["query"]["pages"].Children())
                {
                    builder.Append(children.Children().ToArray()[0]["extract"]);
                }

                if (!String.IsNullOrWhiteSpace(builder.ToString()))
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(ConfigurationManager.AppSettings["output_target"] + topic + ".txt"))
                {
                    file.Write(builder.ToString());
                }
            }

            if (Interlocked.Decrement(ref numThreads) == 0)
                resetEvent.Set();

            Console.WriteLine("Finished " + topic + ".");
        }
    }
}
