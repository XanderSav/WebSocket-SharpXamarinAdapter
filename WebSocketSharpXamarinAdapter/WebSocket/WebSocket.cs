using System;
using System.Diagnostics;
using System.Security.Authentication;
using WebSocketSharp;
using WebSocketSharp.Net;
using Socket = WebSocketSharp.WebSocket;

namespace WebSocketSharpXamarinAdapter.WebSocket
{
    public class WebSocket : IWebSocket
    {
        private Socket _socket;
        private bool _isSubscribed;

        public WebSocketState? State => _socket?.ReadyState;
        public bool? IsAlive => _socket?.IsAlive;


        public WebSocket(string uri, params string[] protocols)
        {
            try
            {
                _socket = new Socket(uri)
                {
                    SslConfiguration = { EnabledSslProtocols = SslProtocols.Tls12 },
                    EnableRedirection = true,
                };
            }
            catch (ArgumentNullException e)
            {
                Debug.WriteLine($"Web socket null uri exception: {e}");
                throw;
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"Web socket creation exception: {e}");
                throw;
            }
        }

        public void Subscribe()
        {
            _socket.OnMessage += _socket_OnMessage;
            _socket.OnOpen += _socket_OnOpen;
            _socket.OnError += _socket_OnError;
            _socket.OnClose += _socket_OnClose;
            _isSubscribed = true;
            //_socket.Log.Level = LogLevel.Debug;
        }

        public void Unsubscribe()
        {
            if (!_isSubscribed) return;
            _socket.OnMessage -= _socket_OnMessage;
            _socket.OnOpen -= _socket_OnOpen;
            _socket.OnError -= _socket_OnError;
            _socket.OnClose -= _socket_OnClose;
            _isSubscribed = false;
        }

        #region Public Events

        /// <summary>
        /// Occurs when the WebSocket connection has been closed.
        /// </summary>
        public event EventHandler<CloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> gets an error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> receives a message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the WebSocket connection has been established.
        /// </summary>
        public event EventHandler OnOpen;

        #endregion

        #region Public Events Calls 

        private void _socket_OnClose(object sender, CloseEventArgs e)
        {
            OnClose?.Invoke(sender, e);
        }

        private void _socket_OnError(object sender, ErrorEventArgs e)
        {
            OnError?.Invoke(sender, e);
        }

        private void _socket_OnOpen(object sender, EventArgs e)
        {
            OnOpen?.Invoke(sender, e);
        }

        private void _socket_OnMessage(object sender, MessageEventArgs e)
        {
            OnMessage?.Invoke(sender, e);
        }

        #endregion

        #region Methods Call

        public void Abort()
        {
            (_socket as IDisposable).Dispose();
            _socket = null;
        }

        public void Close(CloseStatusCode closeStatus, string reason)
        {
            _socket?.Close(closeStatus, reason);
        }

        public void ConnectAsync()
        {
            _socket?.ConnectAsync();
        }

        public void Send(string data)
        {
            _socket?.Send(data);
        }

        public void SetCookie(Cookie cookie)
        {
            _socket?.SetCookie(cookie);
        }

        #endregion

    }
}
