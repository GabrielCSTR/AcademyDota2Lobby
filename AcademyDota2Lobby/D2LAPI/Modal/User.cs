using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2LAPI.Modal
{
    public class User
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public UserInfo user { get; set; }
        public httpResponseAPI Response { get; set; }


        public class UserInfo
        {
            public int id { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public int status { get; set; }
            public int roleid { get; set; }
            public string steam { get; set; }
            public string area { get; set; }
            public string twitchtv { get; set; }
            public string discord { get; set; }
            public string facebook { get; set; }

        }
    }
}
