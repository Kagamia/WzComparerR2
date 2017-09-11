using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("9")]
    sealed class PackSendChat
    {
        public object Message { get; set; }
        public ChatGroup Group { get; set; }
    }

    enum ChatGroup
    {
        Public = 0,
    }
}
