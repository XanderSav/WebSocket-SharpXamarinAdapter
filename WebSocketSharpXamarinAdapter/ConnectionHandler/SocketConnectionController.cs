using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharpXamarinAdapter.DTO;
using WebSocketSharpXamarinAdapter.ReconnectionControllers;
using WebSocketSharpXamarinAdapter.WebSocket;

namespace WebSocketSharpXamarinAdapter.ConnectionHandler
{
    public class SocketConnectionController : ISocketConnectionController
    {
        private IWebSocketImplementation _socket;
        private bool _isInited;

        public event Action<TaskCompletionSource<SocketParameters>> NeedConnection;
        public event Action NeedReconnection;
        public event Action Connecting;
        public event Action Connected;
        public event Action Disconnected;
        public event Action SocketReadyForReconnect;
        public event Action<string> OnMessage;
        public event Action SocketClosedByUser;

        private TaskCompletionSource<SocketParameters> _socketParametersCts;
        private SocketParameters _socketParameters;
        private ITimer _reopenTimer;
        private ushort ReopenInterval = 5;
        private bool _isConnected;
        private bool _isClosedByInternet;

        public void Init()
        {
            if (_isInited) return;
            _socket = new WebSocketImplementation();
            _reopenTimer = new TimerController();
            _socket.Closed += SocketClosed;
            _reopenTimer.Elapsed += _reopenTimer_Elapsed;
            _socket.OnMessage += Socket_OnMessage;
            _socket.NeedReconnection += Socket_NeedReconnection;
            _isInited = true;
        }

        public async Task<bool> Connect()
        {
            if (_socket.SocketState == WebSocketSharp.WebSocketState.Open) return true;
            try
            {
                Connecting?.Invoke();
                _socketParametersCts = new TaskCompletionSource<SocketParameters>();
                NeedConnection?.Invoke(_socketParametersCts);
                _socketParameters = await _socketParametersCts.Task;
                _socket.Init(_socketParameters);
                if (await _socket.Open())
                {
                    Connected?.Invoke();
                    _isConnected = true;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                Debug.WriteLine("Connection failed, Network unreachable");
                throw;
            }
        }

        public void CloseByBgd()
        {
            if (_socket.SocketState != WebSocketSharp.WebSocketState.Open) return;
            _socket.Close(false);
        }
        public void Close(bool? isBackground)
        {
            _socket.Close(isBackground);
        }

        public void OnReadyForBlueGreenDeployment()
        {
            _socket.IsReadyForBgd = true;
        }

        public void Send(JObject data)
        {
            _socket.Send(data);
        }

        public void StartReopenTimer()
        {
            _reopenTimer.Start(ReopenInterval);
        }

        public void StopReopenTimer()
        {
            if (_isClosedByInternet) return;
            _reopenTimer.Stop();
        }

        public void CloseByInternet()
        {
            _isClosedByInternet = true;
            if (_isConnected)
            {
                Disconnected?.Invoke();
            }
            _socket.CloseByInternet();
        }

        #region Listners

        private async void _reopenTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var result = await Connect();
            if (result)
            {
                _reopenTimer.Stop();
                return;
            }

            _reopenTimer.Stop();
            _reopenTimer.Start(ReopenInterval);
        }

        private void SocketClosed(DisconnectedReason reason)
        {
            if (_isConnected)
            {
                Disconnected?.Invoke();
            }
            _isConnected = false;
            
            switch (reason)
            {
                case DisconnectedReason.Background:
                    break;
                case DisconnectedReason.BGD:
                    SocketReadyForReconnect?.Invoke();
                    break;
                case DisconnectedReason.AuthError:
                    break;
                case DisconnectedReason.Unknown:
                    _reopenTimer.Start(ReopenInterval);
                    break;
                case DisconnectedReason.User:
                    SocketClosedByUser?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
        }

        private void Socket_OnMessage(string msgBody)
        {
            OnMessage?.Invoke(msgBody);
        }

        private void Socket_NeedReconnection()
        {
            NeedReconnection?.Invoke();
        }
        #endregion
    }
}
