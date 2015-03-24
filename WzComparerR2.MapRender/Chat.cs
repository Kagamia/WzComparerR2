using System;
using System.Collections.Generic;
using System.Text;
//using PokeIn.Comet;

namespace WzComparerR2.MapRender
{
    /*
    public class Chat
    {
        public Chat()
        {
            this.caller = new APICaller();
            this.client = new DesktopClient(caller, "http://kagamia.sitecloud.cytanium.com/wcweb/chat.ashx", null);
            client.OnClientConnected += (e) =>
            {
                if (this.Connected != null)
                {
                    this.Connected(this, EventArgs.Empty);
                }
            };
            client.OnClientDisconnected += (e) =>
            {
                if (this.Disconnected != null)
                {
                    this.Disconnected(this, EventArgs.Empty);
                }
            };
            client.OnErrorReceived += (o, e) =>
            {
                if (this.Error != null)
                {
                    this.Error(this, new ChatErrorEventArgs() { Error = e });
                }
            };
            this.caller.MessageReceived += (o, e) =>
            {
                if (this.MessageReceived != null)
                {
                    this.MessageReceived(this, e);
                }
            };
        }

        private DesktopClient client;
        private APICaller caller;

        public bool IsConnected
        {
            get { return this.client.IsConnected; }
        }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<ChatErrorEventArgs> Error;
        public event EventHandler<ChatMessageEventArgs> MessageReceived;

        public void Connect()
        {
            client.Connect();
        }

        public void Disconnect()
        {
            client.Close();
            client.Dispose();
        }

        public void Talk(string message)
        {
            if (client.IsConnected)
            {
                client.SendAsync("Chat.Talk", message);
            }
        }

        public void SetName(string name)
        {
            if (client.IsConnected)
            {
                client.SendAsync("Chat.SetName", name);
            }
        }

        public class APICaller
        {
            public event EventHandler<ChatMessageEventArgs> MessageReceived;

            public void OnMessage(int type, string from, string to, string msg)
            {
                if (this.MessageReceived != null)
                {
                    ChatMessageEventArgs e = new ChatMessageEventArgs();
                    e.MsgType = type;
                    e.FromName = from;
                    e.ToName = to;
                    e.MessageText = msg;
                    this.MessageReceived(this, e);
                }
            }
        }
    }

    public class ChatErrorEventArgs : EventArgs
    {
        public string Error { get; set; }
    }

    public class ChatMessageEventArgs : EventArgs
    {
        public int MsgType { get; set; }
        public string FromName { get; set; }
        public string ToName { get; set; }
        public string MessageText { get; set; }
    }
     */
}
