using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharpXamarinAdapter.DTO;
using WebSocketSharpXamarinAdapter.ReconnectionControllers;
using WebSocketSharpXamarinAdapter.WebSocket.StompHelper;
using Timer = System.Timers.Timer;

namespace WebSocketSharpXamarinAdapter.WebSocket
{
    public class WebSocketImplementation : IWebSocketImplementation
    {
        private const string VHost = "trading";
        private const string ExchangeName = "CMD";
        private string _queueName = "session.";
        private const int KeepAliveRequestInterval = 30;
        private const int SocketAbortTimeout = 20;
        private const string StompHeartBeatInterval = "0,5000"; //0 - client will not send, 5000 - server will send every 5 seconds
        private readonly string StompVersion = "1.2";
        private bool _isSubscribed;
        private bool _isConnected;
        private Timer _keepAliveRequestTimer;
        private CookieContainer _cookieContainer;

        private SocketParameters _socketParameters;
        private TaskCompletionSource<bool> _stompConnectedCompletionSource;
        private ITimer _timer;
        private readonly IStompMessageSerializer _serializer = new StompMessageSerializer();
        private IWebSocket _webSocket;

        public bool IsReadyForBgd { get; set; }
        public WebSocketState? SocketState => _webSocket?.State ?? WebSocketState.Closed;

        public event Action<string> OnMessage;
        public event Action NeedReconnection;
        public event Action<DisconnectedReason> Closed;

        public void Init(SocketParameters socketParameters)
        {
            _socketParameters = socketParameters ?? throw new ArgumentNullException(nameof(socketParameters));
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var serverId = GenerateServerId();
            var sessionId = GenerateSessionId();
            _webSocket = new WebSocket($"wss://{_socketParameters.Host}/stomp/{serverId}/{sessionId}/websocket", null);
            _stompConnectedCompletionSource = new TaskCompletionSource<bool>();
            _timer = new TimerController();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (SocketState == WebSocketState.Closed || IsReadyForBgd) return;
            Abort(DisconnectedReason.Unknown);
            _timer.Dispose();
        }

        public async Task<bool> Open()
        {
            if (_webSocket == null) return false;

            SubscribeListeners();
            if (_socketParameters.WebSrv != null)
            {
                _webSocket.SetCookie(new WebSocketSharp.Net.Cookie("WEBSRV", _socketParameters.WebSrv, "/", _socketParameters.Domain));
            }
            _webSocket.ConnectAsync();
            _isConnected = await _stompConnectedCompletionSource.Task;
            if (_isConnected)
            {
                SetCookies();
                StartKeepAliveRequesting();
            }

            return _isConnected;
        }

        public void Send(JObject data)
        {
            var getSettingsMsg = new StompMessage(StompFrame.SEND, data?.ToString())
            {
                ["destination"] = $"/exchange/{ExchangeName}/"
            };
            var reqRes = new JArray(_serializer.Serialize(getSettingsMsg));
            _webSocket?.Send(reqRes.ToString());
        }

        #region Socket Closing
        public async void Close(bool? enteringBackground = null)
        {
            if (_webSocket == null) return;
            if (_webSocket.State != WebSocketState.Open) return;

            try
            {
                await CloseConnection().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                if (enteringBackground == null)
                {
                    Abort(DisconnectedReason.User);
                }
                else
                {
                    Abort((bool)enteringBackground ? DisconnectedReason.Background : DisconnectedReason.BGD);
                }
            }
        }

        public void CloseByInternet()
        {
            Abort(DisconnectedReason.Unknown);
        }

        private Task CloseConnection()
        {
            var socketStableSource = new CancellationTokenSource(2000);
            return Task.Run(() =>
            {
                _webSocket?.Close(CloseStatusCode.Normal, "Close by client");
                Debug.WriteLine("Socket closed by client");
            }, socketStableSource.Token);
        }

        private void Abort(DisconnectedReason reason)
        {
            _isConnected = false;
            UnsubscribeListeners();
            _webSocket?.Abort();
            _webSocket = null;
            _keepAliveRequestTimer.Elapsed -= InitKeepAliveRequest;
            _keepAliveRequestTimer.Stop();
            Closed?.Invoke(reason);
        }

        #endregion

        #region KeepAlive and Cookies 

        private void StartKeepAliveRequesting()
        {
            _keepAliveRequestTimer = new Timer
            {
                Interval = TimeSpan.FromSeconds(KeepAliveRequestInterval).TotalMilliseconds,
                AutoReset = true
            };
            _keepAliveRequestTimer.Elapsed += InitKeepAliveRequest;
            _keepAliveRequestTimer.Enabled = true;
            _keepAliveRequestTimer.Start();
        }

        private void InitKeepAliveRequest(object sender, ElapsedEventArgs e)
        {
#pragma warning disable CS4014 // We don't need to await this call
            SendKeepAliveRequest();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task SendKeepAliveRequest()
        {

            var request = $"https://{_socketParameters.Host}/keepalive?sid={_socketParameters.Session}";

            using (var handler = new HttpClientHandler() { CookieContainer = _cookieContainer })
            using (var client = new HttpClient(handler))
            {
                try
                {
                    var keepAliveResult = await client.GetAsync(request);
                    if (!keepAliveResult.IsSuccessStatusCode)
                        Debug.WriteLine($"Keep Alive Status Code : {keepAliveResult.StatusCode}, {keepAliveResult.ReasonPhrase} ");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exceptiion during keep alive request " + ex.StackTrace);
                }
            }
        }

        private void SetCookies()
        {
            _cookieContainer = new CookieContainer();
            var uri = new Uri($"https://{_socketParameters.Host}");

            _cookieContainer.Add(uri, new Cookie("um_session", _socketParameters.UmId));
            _cookieContainer.Add(uri, new Cookie("WEBSRV", _socketParameters.WebSrv));
        }

        #endregion

        #region Socket Listeners
        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            Debug.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + " WebSocketOnOpen says: " + e);

            ConnectStomp();
            SubscribeStomp();
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + " WebSocketOnError says: " + e.Message);
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            Debug.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + " Socket closed: " + $"{e.Code} Reason: {e.Reason}. Clean close={e.WasClean}");
            if (Enum.IsDefined(typeof(CloseStatusCode), e.Code))
            {
                switch ((CloseStatusCode)e.Code)
                {
                    case CloseStatusCode.Abnormal:
                        if (e.Reason == "An exception has occurred while connecting.")
                        {
                            _stompConnectedCompletionSource.TrySetResult(false); //Unable to open socket connection
                            Abort(DisconnectedReason.AuthError);
                        }
                        else if (e.Reason == "An exception has occurred while receiving.")
                        {
                            Abort(DisconnectedReason.Unknown);
                        }
                        return;
                    case CloseStatusCode.Normal:
                        return;
                    default:
                        if (IsReadyForBgd)
                        {
                            //Abort(DisconnectedReason.BGD);
                            IsReadyForBgd = false;
                            return;
                        }
                        break;
                }
            }

            Abort(DisconnectedReason.Unknown);
        }
        private void WebSocketOnMessage(object sender, MessageEventArgs e)
        {
            if (_isConnected)
            {
                _timer.Stop();
                _timer.Start(SocketAbortTimeout);
            }

            var msgType = e.Data.Substring(0, 1);
            var msgBody = e.Data.Substring(1);
            JToken payloadJson = null;

            switch (msgType)
            {
                case "o":
                    Debug.WriteLine("Opened message was received");
                    return;
                case "h":
                    Debug.WriteLine("Heartbeat received");
                    return;
            }

            if (!string.IsNullOrEmpty(msgBody))
            {
                try
                {
                    payloadJson = JToken.Parse(msgBody);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Socket message parsing failed: {exception}");
                    return;
                }
            }

            if (payloadJson != null && payloadJson.HasValues)
            {
                foreach (var item in payloadJson.Children())
                {
                    var payload = _serializer.Deserialize(item.Value<string>());

                    switch (payload.Command)
                    {
                        case StompFrame.CONNECTED:
                            //CONNECTED frame confirmed. STOMP connected. Socket ready to work.
                            Debug.WriteLine("Socket is now connected, waiting for subscription to queue");
                            return;
                        case StompFrame.MESSAGE:
                            if (!_stompConnectedCompletionSource.Task.IsCompleted)
                            {
                                _stompConnectedCompletionSource.TrySetResult(true); //Definitely authorized
                            }
                            break;
                        case StompFrame.ERROR:
                            var m = payload.Headers.ContainsKey("message") ? payload.Headers["message"] : "";
                            Debug.WriteLine($"Socket message error: {payload.Command}, {payload.Body}, {m}");
                            if (payload.Headers.ContainsKey("message") && payload.Headers["message"] == "Bad CONNECT")
                            {
                                _stompConnectedCompletionSource.TrySetResult(false); //CONNECT frame invalid. Socket closed.
                                Abort(DisconnectedReason.AuthError);
                                return;
                            }
                            if (payload.Headers.ContainsKey("message") && payload.Headers["message"] == "not_found")
                            {
                                //SUBSCRIBE frame invalid. Unable to subscribe to queue. Socket closed.
                                _stompConnectedCompletionSource.TrySetResult(false);
                                Abort(DisconnectedReason.AuthError);
                                NeedReconnection?.Invoke();
                                return;
                            }
                            break;
                    }

                    OnMessage?.Invoke(payload.Body);
                }
            }
        }

        #endregion

        #region Socket subscription handling
        private void SubscribeListeners()
        {
            if (_webSocket == null || _isSubscribed) return;

            _timer.Elapsed += _timer_Elapsed;
            _webSocket.Subscribe();
            _webSocket.OnMessage += WebSocketOnMessage;
            _webSocket.OnOpen += WebSocketOnOpen;
            _webSocket.OnError += WebSocketOnError;
            _webSocket.OnClose += WebSocketOnClose;
            _isSubscribed = true;
        }

        private void UnsubscribeListeners()
        {
            if (_webSocket == null) return;

            _timer.Elapsed -= _timer_Elapsed;
            _webSocket.Unsubscribe();
            _webSocket.OnMessage -= WebSocketOnMessage;
            _webSocket.OnOpen -= WebSocketOnOpen;
            _webSocket.OnError -= WebSocketOnError;
            _webSocket.OnClose -= WebSocketOnClose;
            _isSubscribed = false;
        }
        #endregion

        #region StompConnection subscribtion
        private void ConnectStomp()
        {
            var connectMsg = new StompMessage(StompFrame.CONNECT)
            {
                ["login"] = _socketParameters.StompUser,
                ["passcode"] = _socketParameters.StompPassword,
                ["host"] = VHost,
                ["accept-version"] = StompVersion,
                ["heart-beat"] = StompHeartBeatInterval
            };

            var serializedConnectMsg = $"[\"{_serializer.Serialize(connectMsg)}\"]";
            _webSocket.Send(serializedConnectMsg);
        }

        private void SubscribeStomp()
        {
            var subscribeMsg = new StompMessage(StompFrame.SUBSCRIBE)
            {
                ["id"] = "sub-0",
                ["destination"] = $"/amq/queue/{_queueName}{_socketParameters.Session}"
            };

            var serializedSubscribeMsg = $"[\"{_serializer.Serialize(subscribeMsg)}\"]";
            _webSocket.Send(serializedSubscribeMsg);
        }
        #endregion

        #region Random IDs generation
        private string GenerateServerId()
        {
            Random getRandom = new Random();
            var maxServerId = 1000;
            var patternCount = maxServerId.ToString().Length;
            var patternList = new List<string>(patternCount);
            var stringPattern = string.Join("0", patternList.ToArray());
            var id = getRandom.Next(maxServerId);
            return id.ToString(stringPattern);
        }

        private string GenerateSessionId()
        {
            Random getRandom = new Random();
            var randomStringChars = "abcdefghijklmnopqrstuvwxyz012345";
            var sessionIdLength = 8;
            var sessionId = new string(Enumerable.Repeat(randomStringChars, sessionIdLength).
                Select(s => s[getRandom.Next(s.Length)]).ToArray());
            return sessionId?.Length == sessionIdLength ? sessionId : "wnds1fdk";
        }
        #endregion
    }
}
