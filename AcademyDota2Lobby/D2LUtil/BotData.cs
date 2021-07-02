using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2LUtil
{
    public class BotData
    {
        public BotData(string _botName, string _botLogin, string _botPassword, string ativo)
        {
            this.BotName        = _botName;
            this.BotLogin       = _botLogin;
            this.BotPassword    = _botPassword;
            this.Ativo          = ativo;
        }

        public int ID { set; get; }
        public string BotName { set; get; }
        public string BotLogin { set; get; }
        public string BotPassword { set; get; }
        public string Ativo { set; get; }
    }
}
