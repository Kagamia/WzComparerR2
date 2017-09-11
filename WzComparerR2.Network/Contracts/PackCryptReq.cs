using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("1")]
    sealed class PackCryptReq
    {
        public byte[] Exponent { get; set; }
        public byte[] Modulus { get; set; }
    }
}
