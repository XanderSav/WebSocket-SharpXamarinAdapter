using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharpXamarinAdapter.ConnectionHandler;

namespace WebSocketSharpXamarinAdapter.BackgroundHandler
{
    public class BackgroundController : IBackgroundController
    {
        private readonly ISocketConnectionController _socketConnectionController;

        private Action SocketClose;
        private CancellationTokenSource _backgroundCancellationSource;
        private ushort BackgroundInterval = 5000;
        private Task _delayTask;

        public BackgroundController(ISocketConnectionController socketConnectionController)
        {
            _socketConnectionController = socketConnectionController ??
                                          throw new ArgumentNullException(nameof(socketConnectionController));
        }

        public void Init(Action socketClose)
        {
            SocketClose = socketClose;
        }

        public async Task EnteredBackground()
        {
            _backgroundCancellationSource = new CancellationTokenSource();
            _delayTask = Task.Delay(BackgroundInterval, _backgroundCancellationSource.Token);
            try
            {
                await _delayTask;
                SocketClose?.Invoke();
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Socket closing was aborted, connection still alive");
            }
        }

        public async Task EnteredForeground()
        {
            TaskStatus status = _delayTask?.Status ?? TaskStatus.RanToCompletion;
            if (status == TaskStatus.RanToCompletion)
            {
                if (!await _socketConnectionController.Connect())
                {
                    _socketConnectionController.StartReopenTimer();
                }
            }
            else
            {
                _backgroundCancellationSource?.Cancel();
            }
        }
    }
}