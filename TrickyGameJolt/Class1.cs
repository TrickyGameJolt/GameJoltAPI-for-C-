using System;
using System.Net;
using System.IO;
using System.Net.Http; 


namespace TrickyGameJolt {
    public class GJAPI {
        static string GET(string url) {
            /*
            string ret = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream)) {
                ret = reader.ReadToEnd();
            }
            */
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) {
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = client.GetAsync("answers?order=desc&sort=activity&site=stackoverflow").Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                //Console.WriteLine("Result: " + result);
                return result;
            }
        }

        static string[] GET_Lines(string url) => GET(url).Split('\n');
    }
}
