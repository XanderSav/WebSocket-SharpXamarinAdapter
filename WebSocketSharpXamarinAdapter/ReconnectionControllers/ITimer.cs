using System;
using System.Timers;

namespace WebSocketSharpXamarinAdapter.ReconnectionControllers
{
    public interface ITimer: IDisposable
    {
        bool Enabled { get; }
        void Start(int intervalSeconds);
        void Stop();
        event ElapsedEventHandler Elapsed;
    }
}
