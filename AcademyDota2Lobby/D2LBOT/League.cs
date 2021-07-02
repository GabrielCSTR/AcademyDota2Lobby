using D2LBOT.DotaBot.League;
using D2LBOT.DotaBot.League.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2LBOT
{
    public class League
    {
        #region Private Variables
        private List<LeagueBot> Bots;

        #endregion

        #region Events
        public delegate void BotStatusChangedEvent(int botNumber, LeagueBot bot);
        public event BotStatusChangedEvent OnBotStatusChanged;

        public delegate void BotLobbyChangedEvent(int botNumber, LeagueBot bot);
        public event BotLobbyChangedEvent OnBotLobbyChanged;

        public delegate void BotHeroesPickedEvent(int botNumber, LeagueBot bot, LeagueBotInfoType type, List<string> RadiantHeroes, List<string> DireHeroes);
        public event BotHeroesPickedEvent OnBotHeroesPicked;

        public delegate void BotGameStartedEvent(int botNumber, LeagueBot bot, LeagueBotInfoType type);
        public event BotGameStartedEvent OnBotGameStarted;
        #endregion

        public League()
        {

            Bots = new List<LeagueBot>();

            LeagueBot botInfo;

            botInfo = new LeagueBot("anonymousce", "xaga94hawe", "D2L STR #1");
            botInfo.OnBotStatusChanged += Bot_OnBotStatusChanged;
            botInfo.OnBotLobbyChanged += Bot_OnBotLobbyChanged;
            botInfo.OnBotGameStarted += Bot_OnBotGameStarted;
            botInfo.OnHeroesPicked += Bot_OnHeroesPicked;
            Bots.Add(botInfo);

        }

        private void Bot_OnHeroesPicked(LeagueBot bot, List<string> RadiantHeroes, List<string> DireHeroes)
        {
            if (OnBotHeroesPicked != null)
            {
                OnBotHeroesPicked(Bots.IndexOf(bot), bot, LeagueBotInfoType.HeroList, RadiantHeroes, DireHeroes);
            }
        }

        private void Bot_OnBotGameStarted(LeagueBot bot, BotLobby FinalLobby)
        {
            if (OnBotGameStarted != null)
            {
                OnBotGameStarted(Bots.IndexOf(bot), bot, LeagueBotInfoType.GameStarted);
            }
        }

        private void Bot_OnBotLobbyChanged(LeagueBot bot, BotLobby newLobby)
        {
            if (OnBotLobbyChanged != null)
            {
                OnBotLobbyChanged(Bots.IndexOf(bot), bot);
            }
        }

        private void Bot_OnBotStatusChanged(LeagueBot bot, bool isActive)
        {
            if (OnBotStatusChanged != null)
            {
                OnBotStatusChanged(Bots.IndexOf(bot), bot);
            }
        }
        public LeagueBot[] GetBots()
        {
            return Bots.ToArray();
        }
    }
}
