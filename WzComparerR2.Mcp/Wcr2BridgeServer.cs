using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WzComparerR2.Mcp
{
    public class Wcr2BridgeServer : IDisposable
    {
        private readonly Wcr2Engine engine;
        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool isRunning;
        private long requestCount;

        public Wcr2BridgeServer(Wcr2Engine engine, int port)
        {
            this.engine = engine;
            this.Port = port;
        }

        public int Port { get; private set; }
        public bool IsRunning { get { return this.isRunning; } }
        public long RequestCount { get { return Interlocked.Read(ref this.requestCount); } }

        public void Start()
        {
            if (this.isRunning)
                return;

            this.listener = new TcpListener(IPAddress.Loopback, this.Port);
            this.listener.Start();
            this.Port = ((IPEndPoint)this.listener.LocalEndpoint).Port;
            this.isRunning = true;

            this.acceptThread = new Thread(AcceptLoop);
            this.acceptThread.IsBackground = true;
            this.acceptThread.Name = "Wcr2McpAcceptThread";
            this.acceptThread.Start();
        }

        public void Stop()
        {
            this.isRunning = false;
            try
            {
                if (this.listener != null)
                {
                    this.listener.Stop();
                }
            }
            catch
            {
            }
            this.listener = null;
        }

        private void AcceptLoop()
        {
            while (this.isRunning)
            {
                try
                {
                    TcpClient client = this.listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (SocketException)
                {
                    if (!this.isRunning)
                        break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch
                {
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = (TcpClient)state;
            try
            {
                client.ReceiveTimeout = 300000;
                client.SendTimeout = 300000;
                using (NetworkStream stream = client.GetStream())
                {
                    try
                    {
                        string method;
                        string path;
                        string body;
                        if (!TryReadHttpRequest(stream, out method, out path, out body))
                        {
                            WriteHttpResponse(stream, 400, "{\"ok\":false,\"error\":\"Bad HTTP request\"}");
                            return;
                        }

                        Interlocked.Increment(ref this.requestCount);

                        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                        {
                            WriteHttpResponse(stream, 200, "{\"ok\":true}");
                            return;
                        }

                        if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase))
                        {
                            JObject health = new JObject();
                            health["ok"] = true;
                            health["mode"] = this.engine.IsGuiMode ? "gui" : "headless";
                            health["port"] = this.Port;
                            health["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id;
                            WriteHttpResponse(stream, 200, health.ToString(Formatting.None));
                            return;
                        }

                        if (string.Equals(path, "/status", StringComparison.OrdinalIgnoreCase) && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                        {
                            JObject statusResult = this.engine.Execute("status", new JObject());
                            JObject wrap = new JObject();
                            wrap["ok"] = true;
                            wrap["result"] = statusResult;
                            WriteHttpResponse(stream, 200, wrap.ToString(Formatting.None));
                            return;
                        }

                        string rpcMethod = null;
                        JObject rpcArgs = null;

                        if (!string.IsNullOrEmpty(body))
                        {
                            JObject payload = JObject.Parse(body);
                            if (payload["method"] != null)
                            {
                                rpcMethod = payload["method"].ToString();
                                rpcArgs = (payload["params"] as JObject) ?? (payload["args"] as JObject) ?? new JObject();
                            }
                            else
                            {
                                rpcArgs = payload;
                            }
                        }

                        if (string.IsNullOrEmpty(rpcMethod))
                        {
                            rpcMethod = path.Trim('/');
                            if (rpcMethod.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                            {
                                rpcMethod = rpcMethod.Substring(4);
                            }
                        }

                        if (rpcArgs == null)
                        {
                            rpcArgs = new JObject();
                        }

                        JObject result = this.engine.Execute(rpcMethod, rpcArgs);
                        JObject responseObj = new JObject();
                        responseObj["ok"] = true;
                        responseObj["result"] = result;
                        WriteHttpResponse(stream, 200, responseObj.ToString(Formatting.None));
                    }
                    catch (Exception ex)
                    {
                        Exception realEx = (ex is System.Reflection.TargetInvocationException && ex.InnerException != null)
                            ? ex.InnerException
                            : ex;
                        JObject errObj = new JObject();
                        errObj["ok"] = false;
                        errObj["error"] = realEx.Message;
                        errObj["stackTrace"] = realEx.ToString();
                        WriteHttpResponse(stream, 500, errObj.ToString(Formatting.None));
                    }
                }
            }
            catch
            {
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch
                {
                }
            }
        }

        private static bool TryReadHttpRequest(NetworkStream stream, out string method, out string path, out string body)
        {
            method = null;
            path = null;
            body = null;

            List<byte> headerBytes = new List<byte>(1024);
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                headerBytes.Add((byte)b);
                int count = headerBytes.Count;
                if (count >= 4 &&
                    headerBytes[count - 4] == 13 &&
                    headerBytes[count - 3] == 10 &&
                    headerBytes[count - 2] == 13 &&
                    headerBytes[count - 1] == 10)
                {
                    break;
                }
                if (count > 65536)
                {
                    return false;
                }
            }

            if (headerBytes.Count < 4)
                return false;

            string headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
            string[] lines = headerText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return false;

            string[] firstLine = lines[0].Split(' ');
            if (firstLine.Length < 2)
                return false;

            method = firstLine[0].Trim();
            path = firstLine[1].Trim();
            int qIndex = path.IndexOf('?');
            if (qIndex >= 0)
            {
                path = path.Substring(0, qIndex);
            }

            int contentLength = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon > 0)
                {
                    string hName = lines[i].Substring(0, colon).Trim();
                    string hVal = lines[i].Substring(colon + 1).Trim();
                    if (string.Equals(hName, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(hVal, out contentLength);
                    }
                }
            }

            if (contentLength > 0)
            {
                byte[] bodyBuf = new byte[contentLength];
                int readTotal = 0;
                while (readTotal < contentLength)
                {
                    int n = stream.Read(bodyBuf, readTotal, contentLength - readTotal);
                    if (n <= 0)
                        break;
                    readTotal += n;
                }
                body = Encoding.UTF8.GetString(bodyBuf, 0, readTotal);
            }
            else
            {
                body = "";
            }

            return true;
        }

        private static void WriteHttpResponse(NetworkStream stream, int statusCode, string jsonBody)
        {
            string statusText = statusCode == 200 ? "OK" : (statusCode == 400 ? "Bad Request" : "Internal Server Error");
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            string headers = string.Format(
                "HTTP/1.1 {0} {1}\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                "Content-Length: {2}\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: Content-Type\r\n" +
                "Connection: close\r\n\r\n",
                statusCode, statusText, bodyBytes.Length);
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            stream.Flush();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
