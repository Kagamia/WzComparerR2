using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("2")]
    public sealed class PackCryptResp
    {
        public byte[] KeyEncryptedS2C { get; set; }
        public byte[] KeyEncryptedC2S { get; set; }
    }
}
