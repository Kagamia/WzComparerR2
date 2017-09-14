using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("10")]
    public sealed class PackOnChat
    {
        public string FromID { get; set; }
        public object Message { get; set; }
        public ChatGroup Group { get; set; }
    }
}
