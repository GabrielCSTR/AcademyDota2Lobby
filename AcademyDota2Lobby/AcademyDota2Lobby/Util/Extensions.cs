
using SteamKit2;
using System;

namespace AcademyDota2Lobby.Util
{
    public static class Extensions
    {
        /// <summary>
        ///     Add a callback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manager">callback manager</param>
        /// <param name="cb">callback</param>
        /// <returns></returns>
        public static void Add<T>(this CallbackManager manager, Action<T> cb)
            where T : CallbackMsg
        {
            manager.Subscribe(cb);
        }
    }
}
