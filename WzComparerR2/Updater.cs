using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace WzComparerR2
{
    public class Updater
    {
        public void DownloadUpdate()
        {
            HttpWebRequest webRequest = WebRequest.Create("http://aspspider.info/kagamia/WcDownload.aspx") as HttpWebRequest;
            IAsyncResult result = webRequest.BeginGetResponse(new AsyncCallback(callBack), webRequest);
        }

        private void callBack(IAsyncResult result)
        {
            WebRequest request = result.AsyncState as WebRequest;
            HttpWebResponse resp = null;
            try
            {
                resp = request.EndGetResponse(result) as HttpWebResponse;

            }
            catch (WebException ex)
            {
                if (this.HandleException != null)
                    HandleException(this, new HandleExceptionArgs(ex));
            }
            catch (Exception ex)
            {
                if (this.HandleException != null)
                    HandleException(this, new HandleExceptionArgs(ex));
            }
            finally
            {
                if (resp != null)
                    resp.Close();
            }
        }

        public event EventHandler<HandleExceptionArgs> HandleException;
    }

    public class HandleExceptionArgs : EventArgs
    {
        public HandleExceptionArgs(Exception ex)
        {
            this.ex = ex;
        }

        private Exception ex;

        public Exception Exception
        {
            get { return ex; }
            set { ex = value; }
        }
    }
}
