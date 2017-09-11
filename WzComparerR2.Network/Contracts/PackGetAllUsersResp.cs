using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("8")]
    sealed class PackGetAllUsersResp
    {
        public List<UserInfo> Users { get; set; }
    }

    [JsonObject("8A")]
    sealed class UserInfo
    {
        public ClientType ClientType { get; set; }
        public string UID { get; set; }
        public string NickName { get; set; }
        public DateTime LoginTimeUTC { get; set; }
    }

    enum ClientType
    {
        Unknown = 0,
        WcR2 = 1,
        Web = 2,
    }
}
