/*
using System;
using System.Collections.Generic;
using System.Text;
using SignalR.Client._20.Hubs;

namespace WzComparerR2.MapRender.Net
{
    public class ChatClient
    {
        public ChatClient()
        {
            this.conn = new HubConnection(HubUrl);
            this.hubProxy = this.conn.CreateProxy("chat");

            //注册客户端接口
            this.hubProxy.Subscribe("onMessageReceive").Data += data =>
            {
                if (data == null || data.Length <= 0) return;
                MessageReceiveEventArgs e = new MessageReceiveEventArgs();
                e.ClientID = Convert.ToString(data[0]);
                e.Message = Convert.ToString(data[1]);
                this.OnMessageReceive(e);
            };

            this.hubProxy.Subscribe("onBuddyLogin").Data += data =>
            {
                if (data == null || data.Length <= 0) return;
            };
        }

        private static readonly string HubUrl = "http://localhost:43064/wcweb";

        private HubConnection conn;
        private IHubProxy hubProxy;
        private Subscription messageReceive;

        public event EventHandler<MessageReceiveEventArgs> MessageReceive;

        protected void OnMessageReceive(MessageReceiveEventArgs e)
        {
            if (this.MessageReceive != null)
            {
                this.MessageReceive(this, e);
            }
        }

        public void Connect()
        {
            conn.Start();
        }

        public bool IsActive
        {
            get { return conn.IsActive; }
        }

        public void Disconnect()
        {
            conn.Stop();
        }
    }

    public class MessageReceiveEventArgs : EventArgs
    {
        public string ClientID { get; set; }
        public string Message { get; set; }
    }

    public class BuddyLoginEventArgs : EventArgs
    {
        public string ClientID { get; set; }
    }
}
*/