//using AcademyDota2Lobby.Bot;
using D2LAPI;
using D2LAPI.Modal;
using D2LUtil;
using D2LBOT;
//using SteamKit2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using D2LBOT.DotaBot.League;
using D2LBOT.DotaBot.League.Enum;
using D2LBOT.Class;

namespace AcademyDota2Lobby.Views
{
    /// <summary>
    /// Interaction logic for UCLobby.xaml
    /// </summary>
    public partial class UCLobby : UserControl
    {
       // public static SteamUser.LogOnDetails DotaAuth { get; private set; }
        public string LobbyPass { get; private set; }
        //public DotaBot Bot { get; private set; }

        public static UCLobby THIS;

        private List<Lobby> PlayListAncient;
        private List<Lobby> PlayListDire;

        public ObservableCollection<Lobby> szLobbyAncient;
        public ObservableCollection<Lobby> szLobbyDire;

        private List<BotData> ListBots;

        private League league;

        public UCLobby()
        {
            InitializeComponent();

            DataContext = this;
            THIS = this;

            szLobbyAncient  = new ObservableCollection<Lobby>(); // Dados Lobby Ancient
            szLobbyDire     = new ObservableCollection<Lobby>();    // Dados Lobby Dire
            ListBots        = new List<BotData>();//Lista de bots

            // Criando 5 slot de player
            for (uint i = 1; i <= 5; ++i)
            {
                szLobbyAncient.Add(new Lobby { ID = i, Numero = "Numeric" + i + "Box", Player = "" });
                szLobbyDire.Add(new Lobby { ID = i, Numero = "Numeric" + i + "Box", Player = "" });
            }

            ListViewAncient.ItemsSource = szLobbyAncient;
            ListViewDire.ItemsSource = szLobbyDire;

            cmbModeGame.SelectedIndex = 0;
        }

        private void KickPlayerContextMenu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BanPlayerContextMenu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void KickPlayerDireContextMenu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BanPlayerDireContextMenu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCreateLobby_Click(object sender, RoutedEventArgs e)
        {
            //string modegameSelected = ((ComboBoxItem)cmbModeGame.SelectedItem).Content.ToString(); // modo selecionado
            //string modeGame = "";

            ////switch id do modo do lobby selecionado
            //switch (modegameSelected)
            //{
            //    #region MODOS GAME
            //    /*DOTA_GAMEMODE_NONE = 0,
            //    DOTA_GAMEMODE_AP = 1,
            //    DOTA_GAMEMODE_CM = 2,
            //    DOTA_GAMEMODE_RD = 3,
            //    DOTA_GAMEMODE_SD = 4,
            //    DOTA_GAMEMODE_AR = 5,
            //    DOTA_GAMEMODE_INTRO = 6,
            //    DOTA_GAMEMODE_HW = 7,
            //    DOTA_GAMEMODE_REVERSE_CM = 8,
            //    DOTA_GAMEMODE_XMAS = 9,
            //    DOTA_GAMEMODE_TUTORIAL = 10,
            //    DOTA_GAMEMODE_MO = 11,
            //    DOTA_GAMEMODE_LP = 12,
            //    DOTA_GAMEMODE_POOL1 = 13,
            //    DOTA_GAMEMODE_FH = 14,
            //    DOTA_GAMEMODE_CUSTOM = 15,
            //    DOTA_GAMEMODE_CD = 16,
            //    DOTA_GAMEMODE_BD = 17,
            //    DOTA_GAMEMODE_ABILITY_DRAFT = 18,
            //    DOTA_GAMEMODE_EVENT = 19,
            //    DOTA_GAMEMODE_ARDM = 20,
            //    DOTA_GAMEMODE_1V1MID = 21,
            //    DOTA_GAMEMODE_ALL_DRAFT = 22*/
            //    #endregion
            //    case "AP":
            //        modeGame = "1";
            //        break;
            //    case "CM":
            //        modeGame = "2";
            //        break;
            //    case "AR":
            //        modeGame = "5";
            //        break;
            //    case "X1":
            //        modeGame = "21";
            //        break;
            //    default:
            //        break;
            //}

            //for (int i = 0; i < ListBots.Count; i++)//lista de bots
            //{
            //    if (ListBots[i].Ativo != "1")//pega o bot que nao esta ativo
            //    {
            //        DotaAuth = new SteamUser.LogOnDetails() // salva dados do bot
            //        {
            //            Username = ListBots[i].BotLogin,
            //            Password = ListBots[i].BotPassword
            //        };

            //        ListBots[i].Ativo = "1"; // coloca bot selecionado como ativo
            //        break;
            //    }
            //}

            //// salva lobby password.
            //LobbyPass = GeneratePassword();

            //// thread bot 
            //// criar bot
            //Thread threadBot = new Thread(() => StartBot(DotaAuth, "#AcademyD2", LobbyPass, modeGame));
            //threadBot.Start();

            league = new League();
            league.OnBotStatusChanged += League_OnBotStatusChanged;
            league.OnBotLobbyChanged += League_OnBotLobbyChanged;
            league.OnBotGameStarted += League_OnBotGameStarted;
            league.OnBotHeroesPicked += League_OnBotHeroesPicked;

            LeagueBot[] bots = league.GetBots();
            LeagueBot bot = bots[0];
            Client c = new Client();
            bot.JoinLobby(c, true);

            // logs
            AddLog("##INFORMATION## Lobby Criado!");
            AddLog("##INFORMATION## SENHA: " + LobbyPass);
            AddLog("##NOTICE## Lobby: Esperando jogadores entra no lobby....");

            btnStartLobby.IsEnabled = true;  // ativa botao start lobby
            btnCreateLobby.IsEnabled = false; // desabilita botao criar lobby*/
        }

        private void League_OnBotLobbyChanged(int botNumber, LeagueBot bot)
        {
            AddLog("##INFORMATION## Lobby Criado!");
            //Requests.SendBotUpdate(TCP, botNumber, bot);
        }

        private void League_OnBotStatusChanged(int botNumber, LeagueBot bot)
        {
            //Requests.SendBotUpdate(TCP, botNumber, bot);
        }
        private void League_OnBotGameStarted(int botNumber, LeagueBot bot, LeagueBotInfoType type)
        {
            //Requests.SendBotGameInfo(TCP, botNumber, type, new byte[] { });
        }
        private void League_OnBotHeroesPicked(int botNumber, LeagueBot bot, LeagueBotInfoType type, List<string> RadiantHeroes, List<string> DireHeroes)
        {

            string data = "";
            for (int i = 0; i < 5; i++)
            {
                if (i < RadiantHeroes.Count)
                {
                    if (data != "")
                    {
                        data += "\n";
                    }
                    data += RadiantHeroes[i];
                }
                else
                {
                    if (data != "")
                    {
                        data += "\n";
                    }
                    data += "no_hero";
                }
            }
            for (int i = 0; i < 5; i++)
            {
                if (i < DireHeroes.Count)
                {
                    if (data != "")
                    {
                        data += "\n";
                    }
                    data += DireHeroes[i];
                }
                else
                {
                    if (data != "")
                    {
                        data += "\n";
                    }
                    data += "no_hero";
                }
            }

            //Requests.SendBotGameInfo(TCP, botNumber, type, Encoding.ASCII.GetBytes(data));
        }


        /// <summary>
        /// Start Bot
        /// </summary>
        /// <param name="botid"></param>
        /// <param name="lobbyname"></param>
        /// <param name="lobbypassword"></param>
        //private void StartBot(SteamUser.LogOnDetails botid, string lobbyname, string lobbypassword, string lobbymode)
        //{

        //    // inicia bot
        //    //Bot = new DotaBot(this, botid, lobbyname, lobbypassword, lobbymode);
        //    //Bot.Start();

        //    //Bot = Bot; // seta bot iniciado

        //    // sleep 10s
        //    Thread.Sleep(10);

        //    // logs
        //    AddLog("##INFORMATION## Lobby Criado!");
        //    AddLog("##INFORMATION## SENHA: " + LobbyPass);
        //    AddLog("##NOTICE## Lobby: Esperando jogadores entra no lobby....");

        //    //LogsLobby();
        //}

        /// <summary>
        /// Adiciona log
        /// </summary>
        /// <param name="message"></param>
        public void AddLog(string message)
        {
            if (message != null && message != string.Empty)
            {
                string[] txtSend = message.Split(' ', '\t');
                string msg = "";

                this.txtLogsLobby.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {

                    var paragraph = new Paragraph();
                    if (txtSend[0] == "##NOTICE##")
                    {
                        paragraph.Inlines.Add(new Bold(new Run(txtSend[0] + ": "))
                        {
                            Foreground = Brushes.Green
                        });

                        for (int i = 1; i < txtSend.Length; i++)
                        {
                            msg += txtSend[i] + ' ';
                        }

                        paragraph.Inlines.Add(msg);
                       // paragraph.Inlines.Add(new LineBreak());
                   }
                    else if (txtSend[0] == "##INFORMATION##")
                    {
                        paragraph.Inlines.Add(new Bold(new Run(txtSend[0] + ": "))
                        {
                            Foreground = Brushes.Blue
                        });

                        for (int i = 1; i < txtSend.Length; i++)
                        {
                            msg += txtSend[i] + ' ';
                        }

                        paragraph.Inlines.Add(msg);
                    }
                    else if (txtSend[0] == "##ALERT##")
                    {
                        paragraph.Inlines.Add(new Bold(new Run(txtSend[0] + ": "))
                        {
                            Foreground = Brushes.Red
                        });

                        for (int i = 1; i < txtSend.Length; i++)
                        {
                            msg += txtSend[i] + ' ';
                        }

                        paragraph.Inlines.Add(msg);
                    }
                    else
                    {
                        paragraph.Inlines.Add(message);
                    }

                    txtLogsLobby.Document.Blocks.Add(paragraph);
                }));
            }
        }

        /// <summary>
        /// Update lobby 
        /// </summary>
        /// <param name="player">nick player</param>
        /// <param name="slot">slot lobby</param>
        /// <param name="type"> true|false = true| ancient, false|dire </param>
        public void UpdateLobby(string player, uint slot, bool type)
        {

        }

        /// <summary>
        /// Criar senha random
        /// </summary>
        /// <returns>senha</returns>
        private string GeneratePassword()
        {
            string senha = "";
            Random rand = new Random();

            string[] possibleChars = {"B", "D", "E", "F", "G", "H", "C", "J", "K", "L", "M", "N", "P", "Q",
                         "R", "S", "T", "W", "X", "Y", "Z", "2", "3", "5", "6", "7", "8", "9" };
            for (int i = 0; i < 10; i++)
            {
                senha += possibleChars[rand.Next(0, 28)];
            }

            return senha;
        }

        //Class Lobby
        public class Lobby
        {
            public uint ID { get; set; }
            public string Numero { get; set; }
            public string Player { get; set; }

            public override string ToString()
            {
                return this.Player.ToString();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlayListAncient = new List<Lobby>();
            PlayListDire = new List<Lobby>();

            try
            {
                // API
                ApiOperations api = new ApiOperations();
                // VALIDATION API
                List<BotD2> botD2Handler = api.BotD2Handler(MainWindow.CurrentUser);

                if (botD2Handler != null)
                {
                    for (int i = 0; i < botD2Handler.Count; i++)
                    {
                        //add lista dos bots
                        ListBots.Add(
                            new BotData(
                                botD2Handler[i].bot_name,
                                botD2Handler[i].bot_login,
                                botD2Handler[i].bot_password,
                                botD2Handler[i].status
                            ));
                    }
                }

                // desativa botao start lobby
                btnStartLobby.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Logs.Error(ex.Message);
#if DEBUG
                throw ex;
#endif
            }



        }
    }
}
