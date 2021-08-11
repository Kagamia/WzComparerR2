using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;

namespace WzComparerR2
{
    public class DownloadingItem
    {
        public DownloadingItem(string url, string path)
        {
            this.url = url;
            this.path = path;
        }

        string url;
        string path;
        DateTime lastModified;
        long fileLength;
        Thread thread;
        WebResponse response;
        Stream responseStream;

        public string Url
        {
            get { return url; }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public DateTime LastModified
        {
            get { return lastModified; }
        }

        public long FileLength
        {
            get { return fileLength; }
        }

        public void GetFileLength()
        {
            var uri = new Uri(this.url);
            switch (uri.Scheme.ToLower())
            {
                case "http":
                case "https":
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)(3072 | 12288); //TLS1.2/TLS1.3
                    GetFileLengthHttp();
                    break;

                case "ftp":
                    GetFileLengthFtp();
                    break;
            }
        }

        private void GetFileLengthHttp()
        {
            try
            {
                var req = WebRequest.Create(url) as HttpWebRequest;
                req.Timeout = 15000;
                using (var resp = req.GetResponse() as HttpWebResponse)
                {
                    this.lastModified = resp.LastModified;
                    this.fileLength = resp.ContentLength;
                }
            }
            catch (Exception ex)
            {
                this.fileLength = 0;
                throw;
            }
        }

        private void GetFileLengthFtp()
        {
            try
            {
                var req = WebRequest.Create(url) as FtpWebRequest;
                req.Method = WebRequestMethods.Ftp.GetFileSize;
                req.Timeout = 15000;
                using (var resp = req.GetResponse() as FtpWebResponse)
                {
                    this.fileLength = resp.ContentLength;
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            try
            {
                var req = WebRequest.Create(url) as FtpWebRequest;
                req.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                req.Timeout = 15000;
                using (var resp = req.GetResponse() as FtpWebResponse)
                {
                    this.lastModified = resp.LastModified;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void StartDownload()
        {
            if (thread == null)
            {
                thread = new Thread(tryStartDownload);
                thread.Start();
            }
        }

        private void tryStartDownload()
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Timeout = 15000;
                response = request.GetResponse();
                responseStream = response.GetResponseStream();
                response.Close();
            }
            catch (Exception)
            {
            }
            finally
            {
            }
        }

        public void StopDownload()
        {
            if (response != null)
            {
                try
                {
                    response.Close();
                    response = null;
                    thread.Abort();
                    thread = null;
                }
                catch (Exception)
                {
                }
                finally
                {
                }
            }
        }
    }
}
