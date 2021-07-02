using D2LBOT.DotaBot.Dota_2.Enum;
using D2LUtil;
using Dota2.GC;
using Dota2.GC.Dota.Internal;
using DotaMatch.Params;
using DotaMatch.SteamAPI;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace D2LBOT.DotaBot.Dota_2
{
    class DotaClient
    {
        #region Private Variables
        //account stuff
        private string username;
        private string password;

        //message loop
        private bool doLoop;
        private Thread messageLoop;

        //SteamKit/Dota2 classes
        private SteamClient client;
        private SteamUser user;
        private CallbackManager callbacks;
        public DotaGCHandler DotaGcHandler { get; private set; }
        private DotaClientParams parameters;
        private Dota2HeroesRequest herodata;
        private ulong _lobbyChannelId;

        /// <summary>
        /// Joined lobby chat
        /// </summary>
        public event EventHandler JoinedLobbyChat;

        //Status Variables
        private bool Connected;
        private bool LoggedIn;

        //Match data
        private ulong MatchID;

        #endregion

        #region Public Variables



        #endregion

        #region Events
        public delegate void LobbyCreatedEventHandler(ulong LobbyID);
        public event LobbyCreatedEventHandler OnLobbyCreated;

        public delegate void GameStartedEventHandler(ulong MatchID);
        public event GameStartedEventHandler OnGameStarted;

        public delegate void HeroesPickedEventHandler(List<string> RadiantHeroes, List<string> DireHeroes);
        public event HeroesPickedEventHandler OnHeroesPicked;

        public delegate void GameCompletedEventHandler(DotaGameResult Outcome);
        public event GameCompletedEventHandler OnGameFinished;

        public delegate void StatusEventHandler(DotaClientStatus status, string message);
        public event StatusEventHandler OnStatusChanged;
        #endregion

        #region Public Functions
        /// <summary>
        /// Invites the player to the lobby if one exists.
        /// </summary>
        /// <param name="steamid">Player's SteamID64</param>
        public void InviteToLobby(ulong steamid)
        {
            if (DotaGcHandler.Lobby == null)
                return;

            DotaGcHandler.InviteToLobby(steamid);
        }
        /// <summary>
        /// Initializes a DotaClient with the set account and parameters.
        /// </summary>
        /// <param name="username">Bot Account Username</param>
        /// <param name="password">Bot Account Password</param>
        /// <param name="parameters">Custom Client Parameters</param>
        /// <returns></returns>
        public static DotaClient Create(string username, string password, DotaClientParams parameters)
        {
            DotaClient client = new DotaClient(username, password);
            client.SetParams(parameters);
            return client;
        }

        /// <summary>
        /// Initializes a Dota 2 Game Coordinator Bot.
        /// Bot accounts must have SteamGuard OFF.
        /// </summary>
        /// <param name="user">Bot Username</param>
        /// <param name="pass">Bot Password</param>
        public DotaClient(string username, string password)
        {
            herodata = SteamApiRequest.getHeroData();

            this.username = username;
            this.password = password;

            MatchID = 0;
            LoggedIn = false;
            Connected = false;

            client = new SteamClient();
            callbacks = new CallbackManager(client);
            user = client.GetHandler<SteamUser>();

            callbacks.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbacks.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            callbacks.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbacks.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbacks.Subscribe<DotaGCHandler.PracticeLobbySnapshot>(a => HandleLobbyUpdate(a.lobby));
            callbacks.Subscribe<DotaGCHandler.JoinChatChannelResponse>(a =>
            {
                if (DotaGcHandler.Lobby != null && a.result.channel_id != 0 &&
                    a.result.channel_name == "Lobby_" + DotaGcHandler.Lobby.lobby_id)
                {
                    _lobbyChannelId = a.result.channel_id;
                    JoinedLobbyChat?.Invoke(this, EventArgs.Empty);
                }
            });

            messageLoop = new Thread(() => {
                while (doLoop)
                {
                    callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
            });
        }

        /// <summary>
        /// Sets the DotaClient parameters.
        /// </summary>
        /// <param name="parameters">Client Parameters</param>
        public void SetParams(DotaClientParams parameters)
        {
            this.parameters = parameters;
        }

        /// <summary>
        /// Connects to steam & logs in the bot account.
        /// This is SYNCHRONOUS.
        /// </summary>
        public void Connect()
        {
            client.Connect();
            doLoop = true;
            messageLoop.Start();

            // wait for successful connection
            while (true)
            {
                if (Connected) { break; }
            }

            user.LogOn(new SteamUser.LogOnDetails
            {
                Username = username,
                Password = password,
            });

            //wait for successful login
            while (true)
            {
                if (LoggedIn) { break; }
            }

            DotaGCHandler.Bootstrap(client, Dota2.Base.Data.Games.DOTA2);
            DotaGcHandler = client.GetHandler<DotaGCHandler>();

            //wait for successful dota connection
            bool retry = false;
            do
            {
                retry = false;
                if (ConnectToDota())
                {
                    UpdateStatus(DotaClientStatus.Normal, "Dota: Connected.");
                }
                else
                {
                    retry = this.parameters.dota_reconnect;
                    if (retry)
                    {
                        UpdateStatus(DotaClientStatus.Warning, "Dota: Connection Failed. Retrying...");
                        continue;
                    }
                    else
                    {
                        UpdateStatus(DotaClientStatus.Fatal, "Dota: Connection Failed.");
                        return;
                    }
                }

                if (DotaGcHandler.Lobby != null)
                {
                    UpdateStatus(DotaClientStatus.Warning, "Lobby: Already in a lobby. Leaving...");
                    DotaGcHandler.LeaveLobby();
                    while (DotaGcHandler.Lobby == null)
                    {
                        Thread.Sleep(10);
                    }
                    UpdateStatus(DotaClientStatus.Warning, "Dota: Reconnecting...");
                    DotaGcHandler.Stop();
                    Thread.Sleep(7000);
                    retry = true;
                }
            } while (retry);
        }

        /// <summary>
        /// Resets the dota 2 lobby in preperation for a new lobby to be created
        /// </summary>
        public void Reset()
        {

            // try random bs to leave the post-match screen
            DotaGcHandler.AbandonGame();
            DotaGcHandler.LeaveLobby();


        }

        /// <summary>
        /// Creates a custom lobby and waits for the specified DotaLobbyParams to be met.
        /// Starts the match.
        /// Waits for the match to complete.
        /// </summary>
        /// <param name="LobbyName">Name of lobby</param>
        /// <param name="LobbyPassword">Lobby Password</param>
        /// <param name="parameters">Lobby Start Parameters</param>
        public void CreateLobby(string LobbyName, string LobbyPassword, DotaLobbyParams parameters)
        {

            if (DotaGcHandler.Lobby != null)
            {
                UpdateStatus(DotaClientStatus.Warning, "Lobby: Creating a lobby when already in one.");
            }

            CMsgPracticeLobbySetDetails details = new CMsgPracticeLobbySetDetails();
            details.game_name = LobbyName; // name lobby
            details.pass_key = LobbyPassword;   // name password
            details.game_mode = (uint)DOTA_GameMode.DOTA_GAMEMODE_AP; // game mode
            details.server_region = 10; // regiao brasil
            details.dota_tv_delay = LobbyDotaTVDelay.LobbyDotaTV_300; // delay
            details.game_version = DOTAGameVersion.GAME_VERSION_CURRENT; // versao game
            details.visibility = DOTALobbyVisibility.DOTALobbyVisibility_Public;  // lobby visibilidade

            DotaGcHandler.CreateLobby(LobbyPassword, details);

            // wait for lobby to be created
            while (DotaGcHandler.Lobby == null)
            {
                Thread.Sleep(10);
            }

            UpdateStatus(DotaClientStatus.Normal, "Lobby: Lobby Created.");
            UpdateStatus(DotaClientStatus.Normal, "Lobby: Lobby Name: " + LobbyName);
            UpdateStatus(DotaClientStatus.Normal, "Lobby: Lobby Password: " + LobbyPassword);
            UpdateStatus(DotaClientStatus.Normal, "Lobby: Lobby ID: " + DotaGcHandler.Lobby.lobby_id.ToString());

            Thread.Sleep(1000);
            DotaGcHandler.JoinTeam(DOTA_GC_TEAM.DOTA_GC_TEAM_PLAYER_POOL); // move bot to unassigned
            UpdateStatus(DotaClientStatus.Normal, "Lobby: Moved bot to player pool.");

            if (OnLobbyCreated != null)
            {
                OnLobbyCreated(DotaGcHandler.Lobby.lobby_id);
            }



            UpdateStatus(DotaClientStatus.Normal, "Lobby: Waiting for players to connect....");

            List<DateTime> NotificationTimeouts = new List<DateTime>();
            NotificationTimeouts.Add(DateTime.Now.AddMinutes(1));
            NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
            NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
            NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
            NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
            NotificationTimeouts.Reverse();

            while (true)
            {
                List<CDOTALobbyMember> members = DotaGcHandler.Lobby.members;

                int count = 0;
                foreach (CDOTALobbyMember member in members)
                {
                    if (parameters.isReadyPlayer(member))
                    {
                        count++;
                    }
                }
                if (parameters.hasAllPlayers(count))
                {
                    break;
                }
                Thread.Sleep(1000);

                if (NotificationTimeouts.Count == 0)
                {
                    continue;
                    //TODO: cancel the match and reset bot
                }

                if (DateTime.Now > NotificationTimeouts[0])
                {
                    DotaGcHandler.SendChannelMessage(DotaGcHandler.Lobby.lobby_id, "Players have " + NotificationTimeouts.Count.ToString() + " minute" + (NotificationTimeouts.Count == 1 ? "" : "s") + " to join the lobby.");
                    NotificationTimeouts.RemoveAt(0);
                }
            }

            UpdateStatus(DotaClientStatus.Normal, "Lobby: Starting Lobby.");

            DotaGcHandler.LaunchLobby();

            UpdateStatus(DotaClientStatus.Normal, "Match: Waiting for MatchID.");
            while (DotaGcHandler.Lobby.match_id == 0)
            {
                Thread.Sleep(10);
            }

            MatchID = DotaGcHandler.Lobby.match_id;
            if (OnGameStarted != null)
            {
                OnGameStarted(MatchID);
            }

            EMatchOutcome outcome = WaitForMatchEnd();
            UpdateStatus(DotaClientStatus.Normal, "Match: Result: " + DotaGcHandler.Lobby.match_outcome);

            //publish game result
            if (OnGameFinished != null)
            {

                DotaGameResult result = DotaGameResult.Unknown;
                if (outcome == EMatchOutcome.k_EMatchOutcome_RadVictory)
                {

                    result = DotaGameResult.Radiant;

                }
                else if (outcome == EMatchOutcome.k_EMatchOutcome_DireVictory)
                {

                    result = DotaGameResult.Dire;

                }

                OnGameFinished(result);
            }
        }



        #endregion
        #region Private Functions
        private string getHeroName(uint id)
        {
            if (herodata == null || id < 1)
            {
                return "no_hero";
            }
            Hero hero = herodata.result.heroes.ToList<Hero>().Find(h => { return h.id == id; });
            if (hero == null)
            {
                return "no_hero";
            }
            return hero.name;
        }
        private void UpdateStatus(DotaClientStatus status, string message)
        {
            if (OnStatusChanged != null)
            {
                OnStatusChanged(status, message);
            }

        }
        private EMatchOutcome WaitForMatchEnd()
        {
            // wait for start
            // Todo publish match updates (characters/firstblood/status/ect.)
            // GET MEMBER STEAM ID: dota.Lobby.members[0].id
            // GET MEMBER HERO: getHeroName(dota.Lobby.members[0].hero_id)

            while (true)
            {
                if (DotaGcHandler.Lobby == null)
                    break;

                if (DotaGcHandler.Lobby.game_state == DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS)
                    break;
            }
            if (DotaGcHandler.Lobby != null)
            {
                Thread.Sleep(5000);

                List<CDOTALobbyMember> players = DotaGcHandler.Lobby.members;

                List<string> RadiantHeroes = new List<string>();
                List<string> DireHeroes = new List<string>();
                foreach (CDOTALobbyMember player in players)
                {
                    if (player.team == DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS)
                    {
                        string heroName = getHeroName(player.hero_id);
                        RadiantHeroes.Add(heroName);
                    }
                    else if (player.team == DOTA_GC_TEAM.DOTA_GC_TEAM_BAD_GUYS)
                    {
                        string heroName = getHeroName(player.hero_id);
                        DireHeroes.Add(heroName);
                    }
                }
                if (OnHeroesPicked != null)
                {
                    OnHeroesPicked(RadiantHeroes, DireHeroes);
                }
            }

            //wait for end
            while (true)
            {
                if (DotaGcHandler.Lobby == null)
                {
                    UpdateStatus(DotaClientStatus.Fatal, "Match: Lobby does not exist?");
                    break;
                }
                if (DotaGcHandler.Lobby.match_outcome != EMatchOutcome.k_EMatchOutcome_Unknown)
                {
                    return DotaGcHandler.Lobby.match_outcome;
                }

            }
            return EMatchOutcome.k_EMatchOutcome_Unknown;
        }
        private bool ConnectToDota()
        {
            if (DotaGcHandler == null)
                return false;

            DotaGcHandler.Start();
            Thread.Sleep(7000);
            if (DotaGcHandler.Ready)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
        #region Event Callbacks

        private void OnLoggedOn(SteamUser.LoggedOnCallback obj)
        {
            if (obj.Result == EResult.OK)
            {
                UpdateStatus(DotaClientStatus.Normal, "Steam: Logged on.");
                LoggedIn = true;
            }
            else
            {
                LoggedIn = false;
                if (parameters.relogin)
                {
                    UpdateStatus(DotaClientStatus.Warning, "Steam: Login Failed. Retrying...");
                    Thread.Sleep(1000);
                    user.LogOn(new SteamUser.LogOnDetails
                    {
                        Username = username,
                        Password = password,
                    });
                }
                else
                {
                    UpdateStatus(DotaClientStatus.Fatal, "Steam: Login Failed.");
                }
            }
        }


        private void OnLoggedOff(SteamUser.LoggedOffCallback obj)
        {
            UpdateStatus(DotaClientStatus.Normal, "Steam: Logged off.");
            LoggedIn = false;
        }

        private void OnConnected(SteamClient.ConnectedCallback obj)
        {
            UpdateStatus(DotaClientStatus.Normal, "Steam: Connected.");
            Connected = true;
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback obj)
        {
            Connected = false;
            if (parameters.steam_reconnect)
            {
                UpdateStatus(DotaClientStatus.Warning, "Steam: Disconnected. Reconnecting...");
                Thread.Sleep(1000);
                client.Connect();
            }
            else
            {
                UpdateStatus(DotaClientStatus.Normal, "Steam: Disconnected.");
            }
        }
        #endregion

        /// <summary>
        ///     Send a lobby chat message
        /// </summary>
        /// <param name="msg"></param>
        public void SendLobbyMessage(string msg)
        {
            if (_lobbyChannelId == 0) return;
            DotaGcHandler.SendChannelMessage(_lobbyChannelId, msg);
        }

        private void HandleLobbyUpdate(CSODOTALobby lobby)
        {
            if (DotaGcHandler.Lobby == null)
            {

            }

            // tempo de espera para os player entra no lobby
            // 5 mint
            /*if (NotificationTimeouts == null)
            {
                NotificationTimeouts = new List<DateTime>();
                NotificationTimeouts.Add(DateTime.Now.AddMinutes(1));
                NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
                NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
                NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
                NotificationTimeouts.Add(NotificationTimeouts.Last().AddMinutes(1));
                //NotificationTimeouts.Reverse();
            }*/


            // Dados Player connectar no lobby
            //List<CDOTALobbyMember> members = Lobby.members;

            //// verifica se lobby ja foi criado e informa o id do lobby
            //if (_lobbyChannelId != 0)
            //{
            //    Logs.Notice("LobbyChannelID:" + _lobbyChannelId);
            //    Logs.Info("BOT: " + Lobby.members[0].name + " LobbyID: " + _lobbyChannelId);
            //}

            //// verifica se tem mais player no lobby alem do bot.
            //if (Lobby.members.Count > 1 && Lobby.members.Count < 11)
            //{
            //    var lastmember = Lobby.members[Lobby.members.Count - 1].name;
            //    if (lastmember != null && Lobby.members[Lobby.members.Count - 1].team == DOTA_GC_TEAM.DOTA_GC_TEAM_PLAYER_POOL)
            //    {
            //        // Verifica se o player é banido
            //        if (CheckPlayerBanned(Lobby.members[Lobby.members.Count - 1].id.ToString()))
            //        {
            //            // Kick Player
            //            DotaGcHandler.KickPlayerFromLobby(new SteamID(Lobby.members[Lobby.members.Count - 1].id).AccountID);

            //            // Logs File
            //            Logs.Info(
            //                "LobbyID: " + _lobbyChannelId +
            //                " O Player é banido e foi removido do Lobby: " + Lobby.members[Lobby.members.Count - 1].name
            //                );
            //            // logs app
            //            UCLobby.THIS.AddLog(
            //                "##ALERT## LobbyID: " + _lobbyChannelId +
            //                " O Player é banido e foi removido do Lobby : " + Lobby.members[Lobby.members.Count - 1].name
            //                );
            //        }

            //        // msg player entra no lobby
            //        SendLobbyMessage("Olá player " + lastmember + "!!");
            //    }

            //    for (int i = 0; i < Lobby.members.Count; i++)
            //    {

            //        if (Lobby.members[i].team == DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS)
            //        {
            //            // update player lobby ancient
            //            UCLobby.THIS.UpdateLobby(Lobby.members[i].name, Lobby.members[i].slot, true);
            //            // logs
            //            UCLobby.THIS.AddLog(
            //                "##NOTICE## LobbyID: " + _lobbyChannelId +
            //                " Player Entrou no Lobby Radiant : " + Lobby.members[i].name +
            //                " Slot: " + Lobby.members[i].slot
            //                );
            //            // logs
            //            Logs.Info(
            //                "LobbyID: " + _lobbyChannelId +
            //                " Player Entrou no Lobby Radiant : " + Lobby.members[i].name +
            //                " Slot: " + Lobby.members[i].slot
            //                );
            //        }
            //        else if (Lobby.members[i].team == DOTA_GC_TEAM.DOTA_GC_TEAM_BAD_GUYS)
            //        {
            //            // update player lobby dire
            //            UCLobby.THIS.UpdateLobby(Lobby.members[i].name, Lobby.members[i].slot, false);
            //            // logs
            //            UCLobby.THIS.AddLog(
            //                "LobbyID: " + _lobbyChannelId +
            //                " Player Entrou no Lobby Dire : " + Lobby.members[i].name +
            //                " Slot: " + Lobby.members[i].slot
            //                );
            //            Logs.Info(
            //                "LobbyID: " + _lobbyChannelId +
            //                " Player Entrou no Lobby Dire : " + Lobby.members[i].name +
            //                " Slot: " + Lobby.members[i].slot
            //                );
            //        }
            //    }

            //}
            //else
            //    Logs.Notice("Lobby: Esperando por jogadores....");

            // loop até entra os 10 player
            // caso não entre em 5 mint os 10 player o lobby é descriado.
            //while (true)
            //{
            //    // verifica id lobby
            //    //if (_lobbyChannelId == 0)
            //    //    break;   

            //    int count = 0;

            //    // Pegando player que entra no lobby
            //    foreach (CDOTALobbyMember member in members)
            //    {
            //        if (parametersLobby.isReadyPlayer(member))
            //        {
            //            count++;
            //        }
            //    }

            //    // se todos player tiverem no lobby sai do loop e start o game.
            //    if (parametersLobby != null && parametersLobby.hasAllPlayers(count))
            //    {
            //        break;
            //    }

            //    Thread.Sleep(1000);
            //    // wait for lobby to be created

            //    // 5 mint de espera pra todos os player entra no lobby
            //    // se não entra descria o lobby
            //    /*if (NotificationTimeouts.Count == 0)
            //    {
            //        SendLobbyMessage("Os Players não entraro no tempo limite, lobby CANCELADO!");
            //        DotaGcHandler.AbandonGame();
            //        DotaGcHandler.LeaveLobby();
            //        continue;
            //        //TODO: cancel the match and reset bot
            //    }

            //    if (DateTime.Now > NotificationTimeouts[0])
            //    {
            //        SendLobbyMessage("Os Players tem " + NotificationTimeouts.Count.ToString() + " minutos" + (NotificationTimeouts.Count == 1 ? "" : "s") + " para entra no lobby.");
            //        NotificationTimeouts.RemoveAt(0);
            //    }*/
            //}

            //Logs.Notice("Lobby: Starting Lobby.");

            // start lobby
            // DotaGcHandler.LaunchLobby();

            // esperando pelo ID da partida
            Logs.Notice("Match: Waiting for MatchID.");
            //while (DotaGcHandler.Lobby != null && DotaGcHandler.Lobby.match_id == 0)
            //{
            //    Thread.Sleep(10);
            //}

        }
    }
}
