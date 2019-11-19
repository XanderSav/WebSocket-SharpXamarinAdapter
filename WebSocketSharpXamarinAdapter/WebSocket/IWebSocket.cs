using System;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace WebSocketSharpXamarinAdapter.WebSocket
{
    public interface IWebSocket
    {
        event EventHandler<CloseEventArgs> OnClose;
        event EventHandler<ErrorEventArgs> OnError;
        event EventHandler<MessageEventArgs> OnMessage;
        event EventHandler OnOpen;

        WebSocketState? State { get; }
        bool? IsAlive { get; }

        void Subscribe();
        void Unsubscribe();

        void Abort();
        void Close(CloseStatusCode closeStatus, string reason);
        void ConnectAsync();
        void Send(string data);
        void SetCookie(Cookie cookie);
    }
}