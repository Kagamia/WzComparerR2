using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("11")]
    public sealed class PackOnServerMessage
    {
        public MessageType Type { get; set; }
        public object Message { get; set; }
    }

    public enum MessageType
    {
        Normal = 0,
        Error = 1,
    }
}
