using System;
using System.Timers;

namespace WebSocketSharpXamarinAdapter.ReconnectionControllers
{
    public class TimerController : ITimer
    {
        private Timer _timer = new Timer();

        public bool Enabled { get; private set; }
        public event ElapsedEventHandler Elapsed;
        public void Start(int intervalSeconds)
        {
            if (intervalSeconds <= 0) throw new ArgumentException("Interval could not be equal to or less than 0", nameof(intervalSeconds));

            if (_timer == null)
            {
                _timer = new Timer();
            }

            if (_timer.Enabled)
            {
                Enabled = true;
                return;
            }

            _timer.AutoReset = false;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = TimeSpan.FromSeconds(intervalSeconds).TotalMilliseconds;
            _timer.Start();
            Enabled = true;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed?.Invoke(sender, e);
        }

        public void Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer?.Stop();
            Enabled = false;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _timer = null;
            Enabled = false;
        }
    }
}
