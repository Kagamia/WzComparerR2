using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("13")]
    public sealed class PackOnCustomPackage
    {
        public string FromID { get; set; }
        public object Package { get; set; }
    }
}
