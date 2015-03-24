using System;
using System.IO;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Text;

namespace WzComparerR2.Updater
{
    class WebAPI
    {
        private static readonly string Method_Get = "get";
        private static readonly string Method_Post = "post";
        private static readonly string ContentType_UrlEncoded = "application/x-www-form-urlencoded; charset=UTF-8";
        private static readonly string ContentType_Json = "application/json; charset=UTF-8";
        private static readonly string Accept_Json = "application/json, */*";

        public static string Get(string url)
        {
            HttpWebRequest req = CreateReq(url, Method_Get, Accept_Json);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            string body = GetResponseText(resp);
            resp.Close();
            return body;
        }

        public static T Get<T>(string url)
        {
            string respText = Get(url);
            T entity = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(respText);
            return entity;
        }

        public static byte[] GetRaw(string url)
        {
            MemoryStream ms = new MemoryStream();
            GetRaw(url, ms);
            return ms.ToArray();
        }

        public static int GetRaw(string url, Stream outputStream)
        {
            HttpWebRequest req = CreateReq(url, Method_Get, Accept_Json);

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            int total = 0, length;
            Stream stream = resp.GetResponseStream();
            byte[] buffer = new byte[4096];
            while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                outputStream.Write(buffer, 0, length);
                total += length;
            }

            return total;
        }

        public static string Post(string url, NameValueCollection content)
        {
            HttpWebRequest req = CreateReq(url, Method_Post, Accept_Json);
            req.ContentType = ContentType_UrlEncoded;
            if (content != null && content.Count > 0)
            {
                FillPostData(req, content);
            }
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            string body = GetResponseText(resp);
            resp.Close();
            return body;
        }

        public static T Post<T>(string url, NameValueCollection content)
        {
            string respText = Post(url, content);
            T entity = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(respText);
            return entity;
        }

        private static HttpWebRequest CreateReq(string url, string method, string accept)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.Accept = accept;
            return req;
        }

        private static string GetResponseText(HttpWebResponse resp)
        {
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string body = sr.ReadToEnd();
            sr.Close();
            return body;
        }

        private static void FillPostData(HttpWebRequest req, NameValueCollection content)
        {
            StringBuilder sb = new StringBuilder();
            List<string> kv = new List<string>();
            foreach (string key in content.Keys)
            {
                string[] values = content.GetValues(key);
                foreach (string value in values)
                {
                    sb.AppendFormat("{0}={1}&", key, HttpUtility.UrlEncode(value));
                }
            }

            UTF8Encoding utf8 = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter(req.GetRequestStream(), utf8);
            sw.Write(sb.ToString(0, sb.Length - 1));
            sw.Close();
        }
    }
}
