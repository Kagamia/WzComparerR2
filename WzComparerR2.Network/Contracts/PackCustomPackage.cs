using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("12")]
    public sealed class PackCustomPackage
    {
        public SendTarget Target { get; set; }
        public string[] ID { get; set; }
        public object Package { get; set; }
    }

    public enum SendTarget
    {
        None = 0,
        Self = 1,
        ExceptSelf = 2,
        ID = 3,
        ExceptID = 4,
        All = 5
    }
}
