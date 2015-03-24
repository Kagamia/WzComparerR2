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
            WebResponse response = null;
            try
            {
                WebRequest request = WebRequest.Create(url);

                request.Timeout = 15000;
                response = request.GetResponse();

                if (response is HttpWebResponse)
                {
                    HttpWebResponse r = (HttpWebResponse)response;
                    this.lastModified = r.LastModified;
                    this.fileLength = r.ContentLength;
                }
                else if (response is FtpWebResponse)
                {
                    FtpWebResponse r = (FtpWebResponse)response;
                    this.lastModified = r.LastModified;
                    this.fileLength = r.ContentLength;
                }
                else
                {
                    this.fileLength = response.ContentLength;
                }
            }
            catch (Exception ex)
            {
                this.fileLength = 0;
                throw;
            }
            finally
            {
                if (response != null)
                {
                    try
                    {
                        response.Close();
                    }
                    catch
                    {
                    }
                }
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
