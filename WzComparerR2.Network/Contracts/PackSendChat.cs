using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("9")]
    public sealed class PackSendChat
    {
        public object Message { get; set; }
        public ChatGroup Group { get; set; }
    }

    public enum ChatGroup
    {
        Public = 0,
    }
}
