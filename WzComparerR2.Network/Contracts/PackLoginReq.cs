using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("3")]
    public sealed class PackLoginReq
    {
        public string WcID { get; set; }
        public string NickName { get; set; }
    }
}
