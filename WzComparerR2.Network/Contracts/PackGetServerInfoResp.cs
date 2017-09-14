using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("6")]
    public sealed class PackGetServerInfoResp
    {
        public string Version { get; set; }
        public DateTime StartTimeUTC { get; set; }
        public DateTime CurrentTimeUTC { get; set; }
        public int UserCount { get; set; }
    }
}
