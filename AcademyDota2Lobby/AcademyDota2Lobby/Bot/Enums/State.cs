using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcademyDota2Lobby.Bot.Enums
{
    /// <summary>
    /// State
    /// </summary>
    public enum State
    {
        /// <summary>
        ///     Overall container state
        /// </summary>
        Conceived,

        #region SignedOff

        /// <summary>
        ///     Container state for NOT connected to steam
        /// </summary>
        SignedOff,

        /// <summary>
        ///     Waiting for retry attempt
        /// </summary>
        RetryConnection,

        #endregion

        #region Steam

        /// <summary>
        ///     Connecting to Steam / Using steam
        /// </summary>
        Steam,

        #region DOTA

        /// <summary>
        ///     Currently using DOTA2
        /// </summary>
        Dota,

        /// <summary>
        ///     Connecting to DOTA2
        /// </summary>
        DotaConnect,

        /// <summary>
        ///     Main menu of DOTA
        /// </summary>
        DotaMenu,

        /// <summary>
        ///     Currently in lobby UI
        /// </summary>
        DotaLobby,

        /// <summary>
        ///     Currently in party UI
        /// </summary>
        DotaParty,

        /// <summary>
        ///     Currently game in progress
        /// </summary>
        DotaPlay,

        #endregion

        #endregion
    }
}
