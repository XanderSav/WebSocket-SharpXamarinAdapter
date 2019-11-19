using System;
using System.Threading.Tasks;

namespace WebSocketSharpXamarinAdapter.BackgroundHandler
{
    public interface IBackgroundController
    {
        /// <summary>
        /// Takes socket closing delegate as parameter
        /// </summary>
        /// <param name="socketClose"></param>
        void Init(Action socketClose);

        /// <summary>
        /// On entering background waits for 5 sec to cancelling by calling EnteredForeground method.
        /// Otherwise calls socket closing delegate
        /// </summary>
        /// <returns></returns>
        Task EnteredBackground();

        /// <summary>
        /// Attempts to connect socket if app was in background over then 5 sec.
        /// If attempt was unsuccessful starts reopen timer
        /// </summary>
        /// <returns></returns>
        Task EnteredForeground();
    }
}