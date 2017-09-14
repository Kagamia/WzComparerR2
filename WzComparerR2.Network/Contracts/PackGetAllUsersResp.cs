using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WzComparerR2.Network.Contracts
{
    [JsonObject("8")]
    public sealed class PackGetAllUsersResp
    {
        public List<UserInfo> Users { get; set; }
    }

    [JsonObject("8A")]
    public sealed class UserInfo
    {
        public ClientType ClientType { get; set; }
        public string SID { get; set; }
        public string UID { get; set; }
        public string NickName { get; set; }
        public DateTime LoginTimeUTC { get; set; }
    }

    public enum ClientType
    {
        Unknown = 0,
        WcR2 = 1,
        Web = 2,
    }
}
