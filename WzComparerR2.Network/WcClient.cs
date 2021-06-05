using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using WzComparerR2.Network.Contracts;

namespace WzComparerR2.Network
{
    public class WcClient
    {
        static WcClient()
        {
            DefaultSerializerSetting = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Binder = new TypeNameBinder(),
                Converters = new[] { new ByteArrayConverter() },
            };
        }

        public WcClient()
        {

        }

        public event EventHandler Connected;
        public event EventHandler<ErrorEventArgs> ConnectFailed;
        public event EventHandler Disconnected;
        public event EventHandler<PackEventArgs> OnPackReceived;

        public static readonly JsonSerializerSettings DefaultSerializerSetting;

        public bool IsConnected => this.client?.Connected ?? false;
        public string Host { get; set; }
        public int Port { get; set; }
        public bool AutoReconnect { get; set; }

        private TcpClient client;
        private Task connectTask;
        private Task disconnectTask;
        private Task readTask;
        private Task writeTask;
        private BlockingCollection<object> writeQueue;
        private ICryptoTransform writeCrypto;
        private ICryptoTransform readCrypto;

        public async Task Connect()
        {
            this.connectTask = BeginConnect();
            await this.connectTask;
        }

        private void Reconnect(int delay = 0)
        {
            var reConnTask = Task.Run(async () =>
            {
                await Task.Delay(delay);
                Log.Debug("Begin reconnect.");
                await Connect();
            });
        }

        public void BeginCrypto(ICryptoTransform readCrypto, ICryptoTransform writeCrypto)
        {
            this.readCrypto = readCrypto;
            this.writeCrypto = writeCrypto;
        }

        public void Send(object pack)
        {
            this.writeQueue.Add(pack);
        }

        private async Task BeginConnect()
        {
            this.client = new TcpClient()
            {
                ReceiveTimeout = 60000,
                SendTimeout = 10000,
            };
            
            Log.Debug("Begin connect.");
            while (true)
            {
                try
                {
                    await this.client.ConnectAsync(this.Host, this.Port);
                    Log.Info("Connect success.");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error("Connect failed: {0}", ex.Message);
                    var e = new ErrorEventArgs(ex);
                    this.ConnectFailed?.Invoke(this, e);
                    if (AutoReconnect)
                    {
                        await Task.Delay(5000);
                        continue;
                    }
                }
            }

            this.writeQueue = new BlockingCollection<object>(16);
            this.readCrypto = null;
            this.writeCrypto = null;
            var ns = this.client.GetStream();
            this.readTask = BeginRead(ns);
            this.writeTask = BeginWrite(ns);
            this.disconnectTask = WaitForDisconnect();

            this.Connected?.Invoke(this, EventArgs.Empty);
        }

        private async Task BeginRead(Stream ns)
        {
            Log.Debug("Begin read loop.");
            var readBuffer = new RingBufferStream();
            ICryptoTransform transform = null;

            var br = new BinaryReader(readBuffer);
            var buffer = new byte[4096];
            int packLen = -1;
            try
            {
                while (true)
                {
                    int count = await ns.ReadAsync(buffer, 0, buffer.Length);
                    Log.Debug("Read {0} bytes.", count);
                    if (count <= 0)
                        break;
                    readBuffer.Append(buffer, 0, count);
                    
                    while (true)
                    {
                        //切换加密
                        if (transform != this.readCrypto)
                        {
                            transform = this.readCrypto;
                            if (transform == null)
                            {
                                br = new BinaryReader(readBuffer);
                            }
                            else
                            {
                                var cs = new CryptoStream(readBuffer, transform, CryptoStreamMode.Read);
                                br = new BinaryReader(cs);
                            }
                        }
                        if (packLen < 0)
                        {
                            if (readBuffer.Length >= 2)
                            {
                                packLen = br.ReadUInt16();
                                readBuffer.ClearPrevious();
                            }
                        }
                        if (packLen == 0)
                        {
                            continue;
                        }
                        else if (packLen > 0)
                        {
                            if (readBuffer.Length >= packLen)
                            {
                                var pack = DecodePack(br.ReadBytes(packLen));
                                Log.Debug("Read pack: {0}.", pack);

                                if (pack != null)
                                {
                                    var e = new PackEventArgs(pack);
                                    this.OnPackReceived?.Invoke(this, e);
                                }
                                readBuffer.ClearPrevious();
                                packLen = -1;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Read Error: {0}", ex.Message);
            }
            finally
            {
                Log.Debug("End read.");
                try
                {
                    this.writeQueue.CompleteAdding();
                }
                catch
                {
                }
                ns.Close();
            }
        }

        private async Task BeginWrite(Stream ns)
        {
            await Task.Run(async () =>
            {
                Log.Debug("Begin write loop.");
                try
                {
                    object pack = null;
                    while (this.writeQueue.TryTake(out pack, Int32.MaxValue))
                    {
                        Log.Debug("Write Pack: {0}.", pack);
                        var packData = EncodePack(pack);
                        if (this.writeCrypto != null)
                        {
                            this.writeCrypto.TransformBlock(packData, 0, packData.Length, packData, 0);
                        }
                        await ns.WriteAsync(packData, 0, packData.Length);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Write Error: {0}", ex.Message);
                }
                finally
                {
                    this.writeQueue.Dispose();
                    ns.Close();
                }
                Log.Debug("End write.");
            });
        }

        private async Task WaitForDisconnect()
        {
            await Task.WhenAll(this.readTask, this.writeTask);
            Log.Info("Disconnect.");
            this.Disconnected?.Invoke(this, EventArgs.Empty);
            if (AutoReconnect)
            {
                Reconnect();
            }
        }

        private object DecodePack(byte[] packData)
        {
            var json = Encoding.UTF8.GetString(packData);
            object pack = null;
            try
            {
                pack = JsonConvert.DeserializeObject(json, DefaultSerializerSetting);
            }
            catch
            {
                try
                {
                    pack = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json);
                }
                catch(Exception ex)
                {
                    Log.Warn("Decode pack failed: {0}", ex.Message);
                }
            }
            return pack;
        }

        private byte[] EncodePack(object pack)
        {
            var json = JsonConvert.SerializeObject(pack, DefaultSerializerSetting);
            var enc = Encoding.UTF8;
            var packLen = enc.GetByteCount(json);
            if (packLen > UInt16.MaxValue)
            {
                throw new Exception("pack too large.");
            }
            var buffer = new byte[packLen + 2];
            buffer[0] = (byte)(packLen);
            buffer[1] = (byte)(packLen >> 8);
            enc.GetBytes(json, 0, json.Length, buffer, 2);
            return buffer;
        }
    }

    public sealed class PackEventArgs : EventArgs
    {
        public PackEventArgs(object pack)
        {
            this.Pack = pack;
        }

        public object Pack { get; private set; }
    }

    public sealed class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }

        public Exception Exception { get; private set; }
    }
}
