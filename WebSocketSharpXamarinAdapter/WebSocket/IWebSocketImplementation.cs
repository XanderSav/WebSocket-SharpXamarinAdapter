using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharpXamarinAdapter.DTO;

namespace WebSocketSharpXamarinAdapter.WebSocket
{
    public interface IWebSocketImplementation
    {
        bool IsReadyForBgd { get; set; }
        WebSocketState? SocketState { get; }

        event Action<string> OnMessage;
        event Action NeedReconnection;
        event Action<DisconnectedReason> Closed;

        void Init(SocketParameters socketParameters);
        Task<bool> Open();
        void Send(JObject data);
        void Close(bool? enteringBackground = null);
        void CloseByInternet();
    }
}