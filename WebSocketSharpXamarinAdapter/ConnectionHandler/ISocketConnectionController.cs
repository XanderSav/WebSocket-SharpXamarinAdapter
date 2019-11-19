using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharpXamarinAdapter.DTO;

namespace WebSocketSharpXamarinAdapter.ConnectionHandler
{
    public interface ISocketConnectionController
    {
        event Action<TaskCompletionSource<SocketParameters>> NeedConnection;
        event Action NeedReconnection;
        event Action Connecting;
        event Action Connected;
        event Action Disconnected;
        event Action SocketReadyForReconnect;
        event Action<string> OnMessage;
        event Action SocketClosedByUser;

        void Init();
        Task<bool> Connect();
        void Send(JObject data);
        void Close(bool? isBackground);
        void CloseByBgd();
        void OnReadyForBlueGreenDeployment();
        void StartReopenTimer();
        void StopReopenTimer();
        void CloseByInternet();
    }
}