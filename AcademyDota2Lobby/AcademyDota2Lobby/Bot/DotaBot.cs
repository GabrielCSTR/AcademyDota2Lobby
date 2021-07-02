using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dota2.Base.Data;
using Dota2.GC;
using Dota2.GC.Internal;
using log4net;
using Stateless;
using SteamKit2;
using Timer = System.Timers.Timer;
using System.Linq;
using static Dota2.GC.Dota.Internal.CSODOTALobby;
using DotaMatch.Params;
using AcademyDota2Lobby.Bot.Enums;
using Dota2.GC.Dota.Internal;
using System.IO;
using System.Text;
using AcademyDota2Lobby.Views;
using AcademyDota2Lobby.Util;
using D2LUtil;

namespace AcademyDota2Lobby.Bot
{
    /// <summary>
    ///     An instance of a DOTA 2 lobby/game bot
    /// </summary>
    public class DotaBot
    {
        #region Clients

        static string DIR_DATA = Environment.CurrentDirectory + @"\Config\"; // diretorio da pasta DATA

        public List<string> Dire { get; set; }
        public List<string> Radiant { get; set; }
        public string LobbyName { get; private set; }
        public string LobbyPass { get; private set; }
        public string LobbyMode { get; private set; }

        /// <summary>
        ///     Steam client
        /// </summary>
        public SteamClient SteamClient { get; private set; }

        /// <summary>
        ///     Steam users handler
        /// </summary>
        public SteamUser SteamUser { get; private set; }

        /// <summary>
        ///     Steam friends
        /// </summary>
        public SteamFriends SteamFriends { get; private set; }

        /// <summary>
        ///     Dota2 game coordinator
        /// </summary>
        public DotaGCHandler DotaGcHandler { get; private set; }

        /// <summary>
        ///     The lobby before the current update
        /// </summary>
        public CSODOTALobby Lobby { get; private set; }

        /// <summary>
        ///     State of the bot
        /// </summary>
        public Enums.State State => _state.State;

        /// <summary>
        ///     Called when state transitions
        /// </summary>
        public event EventHandler<StateMachine<Enums.State, Trigger>.Transition> StateTransitioned;

        /// <summary>
        ///     Called when the lobby is updated.
        /// </summary>
        public event EventHandler<CSODOTALobby> LobbyUpdate;

        /// <summary>
        ///     Called when invalid credentials
        /// </summary>
        public event EventHandler InvalidCreds;

        /// <summary>
        /// Lobby invite received
        /// </summary>
        public event EventHandler<CSODOTALobbyInvite> LobbyInviteReceived;

        /// <summary>
        /// Party invite received
        /// </summary>
        public event EventHandler<CSODOTAPartyInvite> PartyInviteReceived;

        /// <summary>
        /// Joined lobby chat
        /// </summary>
        public event EventHandler JoinedLobbyChat;

        #endregion

        #region Private

        private readonly SteamUser.LogOnDetails _logonDetails;
        private readonly ILog log;
        private StateMachine<Enums.State, Trigger> _state;
        private readonly Timer _reconnectTimer;
        private CallbackManager _cbManager;
        private readonly bool _shouldReconnect = true;
        private int _cbThreadCtr;
        private Thread _procThread;
        private ulong _lobbyChannelId;

        private readonly Dictionary<ulong, Action<CMsgDOTAMatch>> _callbacks =
            new Dictionary<ulong, Action<CMsgDOTAMatch>>();

        private ulong _matchId;

        private bool _isRunning;

        // Tempo do lobby criado
        private List<DateTime> NotificationTimeouts;

        // Params Lobby
        private DotaLobbyParams parametersLobby;

        //Match data
        private ulong MatchID;

        #endregion

        #region Constructor

        /// <summary>
        ///     Create a new game bot
        /// </summary>
        /// <param name="details">Auth info</param>
        /// <param name="reconnectDelay">
        ///     Delay between reconnection attempts to steam, set to a negative value to disable
        ///     reconnecting
        /// </param>
        /// <param name="contrs">Game controllers</param>
        public DotaBot(UCLobby MainLobby, SteamUser.LogOnDetails details, string lobbyname, string lobbypass, string lobbymode, double reconnectDelay = 2000)
        {

            log = LogManager.GetLogger("Bot " + details.Username);
            log.Debug("Initializing a new LobbyBot w/username " + details.Username);

            _logonDetails = details;

            LobbyName = lobbyname;
            LobbyPass = lobbypass;
            LobbyMode = lobbymode;

            if (reconnectDelay < 0)
            {
                reconnectDelay = 10;
                _shouldReconnect = false;
            }

            _reconnectTimer = new Timer(reconnectDelay);
            _reconnectTimer.Elapsed += (sender, args) => _state.Fire(Trigger.ConnectRequested);

            _state = new StateMachine<Enums.State, Trigger>(Enums.State.SignedOff);
            _state.OnTransitioned((transition =>
            {
                log.DebugFormat("{0} => {1}", transition.Source.ToString("G"), transition.Destination.ToString("G"));
                StateTransitioned?.Invoke(this, transition);
            }));

            _state.Configure(Enums.State.Conceived)
                .Permit(Trigger.ShutdownRequested, Enums.State.SignedOff);

            _state.Configure(Enums.State.SignedOff)
                .SubstateOf(Enums.State.Conceived)
                .Ignore(Trigger.SteamDisconnected)
                .OnEntryFrom(Trigger.SteamInvalidCreds, () => InvalidCreds?.Invoke(this, EventArgs.Empty))
                .PermitIf(Trigger.ConnectRequested, Enums.State.Steam, () => _isRunning);

            _state.Configure(Enums.State.RetryConnection)
                .SubstateOf(Enums.State.SignedOff)
                .OnExit(() => _reconnectTimer.Stop())
                .OnEntry(() => _reconnectTimer.Start())
                .Permit(Trigger.ConnectRequested, Enums.State.Steam);

            _state.Configure(Enums.State.Steam)
                .SubstateOf(Enums.State.Conceived)
                .Permit(Trigger.SteamConnected, Enums.State.Dota)
                .PermitDynamic(Trigger.SteamDisconnected,
                    () => _shouldReconnect ? Enums.State.RetryConnection : Enums.State.SignedOff)
                .Permit(Trigger.SteamInvalidCreds, Enums.State.SignedOff)
                .OnEntry(StartSteamConnection)
                .OnExit(ReleaseSteamConnection);


            _state.Configure(Enums.State.Dota)
                .SubstateOf(Enums.State.Steam)
                .Permit(Trigger.DotaConnected, Enums.State.DotaMenu)
                .PermitReentry(Trigger.DotaDisconnected)
                .Permit(Trigger.DotaEnteredLobbyUI, Enums.State.DotaLobby)
                .Permit(Trigger.DotaEnteredLobbyPlay, Enums.State.DotaPlay)
                .OnEntryFrom(Trigger.SteamConnected, StartDotaGCConnection);

            _state.Configure(Enums.State.DotaMenu)
                .SubstateOf(Enums.State.Dota)
                .Permit(Trigger.DotaEnteredLobbyUI, Enums.State.DotaLobby)
                .Permit(Trigger.DotaEnteredLobbyPlay, Enums.State.DotaPlay)
                .OnEntry(CreateLobby);

            _state.Configure(Enums.State.DotaLobby)
                .SubstateOf(Enums.State.Dota)
                .Ignore(Trigger.DotaEnteredLobbyUI)
                .Permit(Trigger.DotaEnteredLobbyPlay, Enums.State.DotaPlay)
                .Permit(Trigger.DotaNoLobby, Enums.State.DotaMenu)
                .OnEntry(JoinLobbySlot)
                .OnEntry(JoinLobbyChat)
                .OnExit(LeaveLobbyChat);

            _state.Configure(Enums.State.DotaPlay)
                .SubstateOf(Enums.State.Dota)
                .Ignore(Trigger.DotaEnteredLobbyPlay)
                .Permit(Trigger.DotaEnteredLobbyUI, Enums.State.DotaLobby)
                .Permit(Trigger.DotaNoLobby, Enums.State.DotaMenu);
        }

        #endregion

        #region Bot Specific Implementation

        /// <summary>
        ///    move bot to unassigned
        /// </summary>
        private void JoinLobbySlot()
        {
            DotaGcHandler.JoinTeam(DOTA_GC_TEAM.DOTA_GC_TEAM_PLAYER_POOL, 6);
            // DotaGcHandler.JoinBroadcastChannel();

            // invite player para o lobby
            /* if (_lobbyChannelId != 0)
             {
                 InviteToLobby(76561198032447477);
             }*/

        }

        /// <summary>
        ///     Send a lobby chat message
        /// </summary>
        /// <param name="msg"></param>
        public void SendLobbyMessage(string msg)
        {
            if (_lobbyChannelId == 0) return;
            DotaGcHandler.SendChannelMessage(_lobbyChannelId, msg);
        }

        /// <summary>
        /// Check if we are the lobby host
        /// </summary>
        public bool IsLobbyHost => DotaGcHandler.Lobby?.leader_id == SteamClient.SteamID.ConvertToUInt64();

        /// <summary>
        ///     Join the lobby chat channel
        /// </summary>
        private void JoinLobbyChat()
        {
            if (DotaGcHandler.Lobby == null)
            {
                log.Warn("JoinLobbyChat called with no lobby!");
                return;
            }

            DotaGcHandler.JoinChatChannel("Lobby_" + DotaGcHandler.Lobby.lobby_id,
                DOTAChatChannelType_t.DOTAChannelType_Lobby);

        }

        private void CreateLobby()
        {

            //DotaGCHandler.LeaveLobby();
            log.Debug("Requested lobby creation...");

            // seta dados lobby
            CMsgPracticeLobbySetDetails details = new CMsgPracticeLobbySetDetails();

            details.game_name = LobbyName; // name lobby
            details.pass_key = LobbyPass; // pass lobby
            details.game_mode = Convert.ToUInt32(LobbyMode); // game mode
            details.server_region = 10; // regiao brasil
            details.dota_tv_delay = LobbyDotaTVDelay.LobbyDotaTV_300; // delay
            details.game_version = DOTAGameVersion.GAME_VERSION_CURRENT; // versao game 
            details.visibility = DOTALobbyVisibility.DOTALobbyVisibility_Public; // lobby visibilidade

            DotaGcHandler.CreateLobby(LobbyPass, details); // criar lobby

            //InviteToLobby(76561198032447477); // invita player para o lobby
        }


        /// <summary>
        /// Invites the player to the lobby if one exists.
        /// </summary>
        /// <param name="steamid">Player's SteamID64</param>
        public void InviteToLobby(ulong steamid)
        {
            DotaGcHandler.InviteToLobby(steamid);
        }

        /// <summary>
        ///     Leave a lobby chat channel
        /// </summary>
        private void LeaveLobbyChat()
        {
            if (_lobbyChannelId == 0) return;
            DotaGcHandler.LeaveChatChannel(_lobbyChannelId);
            _lobbyChannelId = 0;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Start connecting to Steam
        /// </summary>
        private void StartSteamConnection()
        {
            ReleaseSteamConnection();

            var c = SteamClient = new SteamClient();

            DotaGCHandler.Bootstrap(c, Games.DOTA2);

            SteamUser = c.GetHandler<SteamUser>();
            SteamFriends = c.GetHandler<SteamFriends>();

            var cb = _cbManager = new CallbackManager(c);

            SetupSteamCallbacks(cb);
            SetupDotaGcCallbacks(cb);

            c.Connect();
            _cbThreadCtr++;
            _procThread = new Thread(SteamThread);
            _procThread.Start(this);
        }

        /// <summary>
        ///     Make sure every client is shutdown completely
        /// </summary>
        private void ReleaseSteamConnection()
        {
            ReleaseDotaGCConnection();

            SteamFriends?.SetPersonaState(EPersonaState.Offline);
            SteamFriends = null;

            SteamUser?.LogOff();
            SteamUser = null;

            SteamClient?.Disconnect();
            SteamClient = null;

            _cbThreadCtr++;
        }

        /// <summary>
        ///     Start connecting to the DOTA 2 game coordinator
        /// </summary>
        private void StartDotaGCConnection()
        {
            DotaGcHandler = SteamClient.GetHandler<DotaGCHandler>();
            DotaGcHandler.Start();
        }

        /// <summary>
        ///     Completely disconnect from the DOTA gc
        /// </summary>
        private void ReleaseDotaGCConnection()
        {
            DotaGcHandler?.Stop();
            DotaGcHandler = null;
        }


        private void UpdatePersona()
        {

            var cname = SteamFriends.GetPersonaName();
            var tname = "[BOT]SAD2L #1";
            if (cname != tname)
            {
                log.DebugFormat("Changed persona name to {0} from {1}.", tname, cname);
                SteamFriends.SetPersonaName(tname);
            }

            SteamFriends.SetPersonaState(EPersonaState.Online);
        }

        /// <summary>
        ///     Internal thread
        /// </summary>
        /// <param name="state"></param>
        private static void SteamThread(object state)
        {
            DotaBot bot = state as DotaBot;
            int tid = bot._cbThreadCtr;
            var ts = TimeSpan.FromSeconds(1);
            while (tid == bot._cbThreadCtr)
            {
                try
                {
                    bot._cbManager.RunWaitCallbacks(ts);
                }
                catch (Exception ex)
                {
                    bot.log.Error("Error in Steam thread!", ex);
                }
            }
        }

        #endregion

        #region _callbacks

        /// <summary>
        ///     Setup steam client callbacks
        /// </summary>
        /// <param name="cb"></param>
        private void SetupSteamCallbacks(CallbackManager cb)
        {
            // Handle general connection stuff
            cb.Add<SteamUser.AccountInfoCallback>(a =>
            {
                log.DebugFormat("Current name is: {0}, flags {1}, ", a.PersonaName, a.AccountFlags.ToString("G"));
                UpdatePersona();
            });
            cb.Add<SteamClient.ConnectedCallback>(a => SteamUser?.LogOn(_logonDetails));
            cb.Add<SteamClient.DisconnectedCallback>(a => _state?.Fire(Trigger.SteamDisconnected));
            cb.Add<SteamUser.LoggedOnCallback>(a =>
            {
                log.DebugFormat("Steam signin result: {0}", a.Result.ToString("G"));
                switch (a.Result)
                {
                    case EResult.OK:
                        _state?.Fire(Trigger.SteamConnected);
                        break;

                    case EResult.ServiceUnavailable:
                    case EResult.ServiceReadOnly:
                    case EResult.TryAnotherCM:
                    case EResult.AccountLoginDeniedThrottle:
                    case EResult.AlreadyLoggedInElsewhere:
                    case EResult.BadResponse:
                    case EResult.Busy:
                    case EResult.ConnectFailed:
                        _state?.Fire(Trigger.SteamDisconnected); //retry state
                        break;
                    default:
                        _state?.Fire(Trigger.SteamInvalidCreds);
                        break;
                }
            });
        }

        /// <summary>
        ///     Setup DOTA 2 game coordinator callbacks
        /// </summary>
        /// <param name="cb">Manager</param>
        private void SetupDotaGcCallbacks(CallbackManager cb)
        {
            cb.Add<DotaGCHandler.GCWelcomeCallback>(a =>
            {
                log.Debug("GC session welcomed");
                _state.Fire(Trigger.DotaConnected);
            });
            cb.Add<DotaGCHandler.ConnectionStatus>(a =>
            {
                log.DebugFormat("GC connection status: {0}", a.result.status.ToString("G"));
                _state.Fire(a.result.status == GCConnectionStatus.GCConnectionStatus_HAVE_SESSION
                    ? Trigger.DotaConnected
                    : Trigger.DotaDisconnected);
            });
            cb.Add<DotaGCHandler.Popup>(a => log.DebugFormat("GC popup message: {0}", a.result.id.ToString("G")));
            cb.Add<DotaGCHandler.PracticeLobbySnapshot>(a => HandleLobbyUpdate(a.lobby));
            cb.Add<DotaGCHandler.PracticeLobbyLeave>(a => HandleLobbyUpdate(null));
            cb.Add<DotaGCHandler.JoinChatChannelResponse>(a =>
            {
                if (DotaGcHandler.Lobby != null && a.result.channel_id != 0 &&
                    a.result.channel_name == "Lobby_" + DotaGcHandler.Lobby.lobby_id)
                {
                    _lobbyChannelId = a.result.channel_id;
                    JoinedLobbyChat?.Invoke(this, EventArgs.Empty);
                }
            });
            cb.Add<DotaGCHandler.ChatMessage>(
                a =>
                {
                    log.DebugFormat("[Chat][" +
                                    (a.result.channel_id == _lobbyChannelId ? "Lobby" : a.result.channel_id + "") + "] " +
                                    a.result.persona_name + ": " + a.result.text);
                    if (a.result.channel_id == _lobbyChannelId)
                    {
                        // start game por comando no lobby
                        //if (a.result.text.Contains("!start")) DotaGcHandler.LaunchLobby();
                    }
                });
            cb.Add<DotaGCHandler.MatchResultResponse>(c =>
            {
                Action<CMsgDOTAMatch> cbx;
                ulong id;
                id = c.result.match?.match_id ?? _matchId;
                if (!_callbacks.TryGetValue(id, out cbx)) return;
                _callbacks.Remove(id);
                log.Warn("Match result response for " + id + ": " + ((EResult)c.result.result).ToString("G"));
                cbx(c.result.match);
            });
            cb.Add<DotaGCHandler.LobbyInviteSnapshot>(c =>
            {
                LobbyInviteReceived?.Invoke(this, c.invite);
            });
            cb.Add<DotaGCHandler.PartyInviteSnapshot>(c =>
            {
                PartyInviteReceived?.Invoke(this, c.invite);
            });
        }


        private void HandleLobbyUpdate(CSODOTALobby lobby)
        {
            if (Lobby == null && lobby != null)
            {
                log.DebugFormat("Entered lobby {0} with state {1}.", lobby.lobby_id, lobby.state.ToString("G"));
            }
            else if (Lobby != null && lobby == null)
            {
                log.DebugFormat("Exited lobby {0}.", Lobby.lobby_id);
            }

            if (lobby != null)
                _state.Fire(lobby.state == CSODOTALobby.State.UI || string.IsNullOrEmpty(lobby.connect)
                    ? Trigger.DotaEnteredLobbyUI
                    : Trigger.DotaEnteredLobbyPlay);
            else
                _state.Fire(Trigger.DotaNoLobby);

            LobbyUpdate?.Invoke(Lobby, lobby);
            Lobby = lobby;

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
            List<CDOTALobbyMember> members = Lobby.members;

            // verifica se lobby ja foi criado e informa o id do lobby
            if (_lobbyChannelId != 0)
            {
                Logs.Notice("LobbyChannelID:" + _lobbyChannelId);
                Logs.Info("BOT: " + Lobby.members[0].name + " LobbyID: " + _lobbyChannelId);
            }

            // verifica se tem mais player no lobby alem do bot.
            if (Lobby.members.Count > 1 && Lobby.members.Count < 11)
            {
                var lastmember = Lobby.members[Lobby.members.Count - 1].name;
                if (lastmember != null && Lobby.members[Lobby.members.Count - 1].team == DOTA_GC_TEAM.DOTA_GC_TEAM_PLAYER_POOL)
                {
                    // Verifica se o player é banido
                    if (CheckPlayerBanned(Lobby.members[Lobby.members.Count - 1].id.ToString()))
                    {
                        // Kick Player
                        DotaGcHandler.KickPlayerFromLobby(new SteamID(Lobby.members[Lobby.members.Count - 1].id).AccountID);

                        // Logs File
                        Logs.Info(
                            "LobbyID: " + _lobbyChannelId +
                            " O Player é banido e foi removido do Lobby: " + Lobby.members[Lobby.members.Count - 1].name
                            );
                        // logs app
                        UCLobby.THIS.AddLog(
                            "##ALERT## LobbyID: " + _lobbyChannelId +
                            " O Player é banido e foi removido do Lobby : " + Lobby.members[Lobby.members.Count - 1].name
                            );
                    }

                    // msg player entra no lobby
                    SendLobbyMessage("Olá player " + lastmember + "!!");
                }

                for (int i = 0; i < Lobby.members.Count; i++)
                {

                    if (Lobby.members[i].team == DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS)
                    {
                        // update player lobby ancient
                        UCLobby.THIS.UpdateLobby(Lobby.members[i].name, Lobby.members[i].slot, true);
                        // logs
                        UCLobby.THIS.AddLog(
                            "##NOTICE## LobbyID: " + _lobbyChannelId +
                            " Player Entrou no Lobby Radiant : " + Lobby.members[i].name +
                            " Slot: " + Lobby.members[i].slot
                            );
                        // logs
                        Logs.Info(
                            "LobbyID: " + _lobbyChannelId +
                            " Player Entrou no Lobby Radiant : " + Lobby.members[i].name +
                            " Slot: " + Lobby.members[i].slot
                            );
                    }
                    else if (Lobby.members[i].team == DOTA_GC_TEAM.DOTA_GC_TEAM_BAD_GUYS)
                    {
                        // update player lobby dire
                        UCLobby.THIS.UpdateLobby(Lobby.members[i].name, Lobby.members[i].slot, false);
                        // logs
                        UCLobby.THIS.AddLog(
                            "LobbyID: " + _lobbyChannelId +
                            " Player Entrou no Lobby Dire : " + Lobby.members[i].name +
                            " Slot: " + Lobby.members[i].slot
                            );
                        Logs.Info(
                            "LobbyID: " + _lobbyChannelId +
                            " Player Entrou no Lobby Dire : " + Lobby.members[i].name +
                            " Slot: " + Lobby.members[i].slot
                            );
                    }
                }

            }
            else
                Logs.Notice("Lobby: Esperando por jogadores....");

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
            while (DotaGcHandler.Lobby.match_id == 0)
            {
                Thread.Sleep(10);
            }

        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Start the bot
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            Task.Run(() => _state.Fire(Trigger.ConnectRequested));
        }

        /// <summary>
        ///     Shutdown the bot completely
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            Task.Run(() => _state.Fire(Trigger.ShutdownRequested));
        }

        public void Dispose()
        {
            Stop();
            ReleaseSteamConnection();
            _state = null;
        }

        public void FetchMatchResult(ulong matchId, Action<CMsgDOTAMatch> callback)
        {
            // Set timeout
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                if (!_callbacks.ContainsKey(matchId)) return;
                log.Debug("Match result fetch for " + matchId + " timed out!");
                _callbacks[matchId](null);
                _callbacks.Remove(matchId);
            });

            _matchId = matchId;
            _callbacks[matchId] = callback;
            DotaGcHandler.RequestMatchResult(matchId);
        }

        public void LeaveLobby(bool kickEveryone = false)
        {
            if (kickEveryone && DotaGcHandler.Lobby != null)
                foreach (var pm in DotaGcHandler.Lobby.members)
                    DotaGcHandler.KickPlayerFromLobby(new SteamID(pm.id).AccountID);

            DotaGcHandler.AbandonGame();
            DotaGcHandler.LeaveLobby();
        }

        /// <summary>
        /// Verifica se o player esta na lista de banidos
        /// </summary>
        /// <param name="PlayerSteamID">SteamID64</param>
        /// <returns></returns>
        public bool CheckPlayerBanned(string PlayerSteamID)
        {
            const Int32 BufferSize = 128;
            using (var fileStream = File.OpenRead(DIR_DATA + "Banidos.txt"))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.Contains(PlayerSteamID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Retonar SteamID64 Do player
        /// </summary>
        /// <param name="PlayerName"></param>
        /// <returns></returns>
        public ulong GetSteamID64(string PlayerName)
        {
            // player ID steam64
            ulong PlayerID = 0;

            // verificando players no lobby
            // pegando o ID steam se o player existir no lobby
            foreach (var infoPlayer in DotaGcHandler.Lobby.members)
                if (infoPlayer.name == PlayerName)
                    PlayerID = infoPlayer.id;

            // Verificando se o id do player esta no lobby
            foreach (var pm in DotaGcHandler.Lobby.members)
                if (pm.id == PlayerID)
                {
                    return pm.id;
                }

            return 0;
        }
        /// <summary>
        /// Remover Player do lobby
        /// </summary>
        /// <param name="Player">nick player </param>
        /// <param name="type"> true|false </param>
        public void KickPlayer(string Player, bool type)
        {
            // player ID steam64
            ulong PlayerID = 0;

            // verificando players no lobby
            // pegando o ID steam se o player existir no lobby
            foreach (var infoPlayer in DotaGcHandler.Lobby.members)
                if (infoPlayer.name == Player)
                    PlayerID = infoPlayer.id;


            // true = radiant 
            // false = dire
            if (type)
            {
                foreach (var pm in DotaGcHandler.Lobby.members)
                    if (pm.id == PlayerID)
                    {
                        DotaGcHandler.KickPlayerFromLobby(new SteamID(pm.id).AccountID);
                        Logs.Info(
                            "LobbyID: " + _lobbyChannelId +
                            " Player foi removido do  Lobby Radiant : " + pm.name +
                            " Slot: " + pm.slot
                            );
                        SendLobbyMessage("O Player '" + pm.name + "' foi removido do lobby!!");
                    }
            }
            else
            {
                foreach (var pm in DotaGcHandler.Lobby.members)
                    if (pm.id == PlayerID)
                    {
                        DotaGcHandler.KickPlayerFromLobby(new SteamID(pm.id).AccountID);
                        Logs.Info(
                            "LobbyID: " + _lobbyChannelId +
                            " Player foi removido do  Lobby Dire : " + pm.name +
                            " Slot: " + pm.slot
                            );
                        SendLobbyMessage("O Player '" + pm.name + "' foi removido do lobby!!");
                    }
            }

        }
        #endregion
    }


}
