using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharpXamarinAdapter.ReconnectionControllers;

namespace NUnit_Tests_WS.WebSocketTest
{
    [TestFixture]
    public class TimerControllerTest
    {
        private ITimer _timer;
        [SetUp]
        public void Setup()
        {
            _timer = new TimerController();
            _wasInvoked = false;
        }

        [TestCase(1, 1000)]
        [TestCase(3, 3000)]
        [TestCase(5, 5000)]
        [TestCase(10, 10000)]
        public void IntervalPositiveTest(int interval, double expected)
        {
            _timer.Start(interval);

            var field = typeof(TimerController).GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
            var timer = field?.GetValue(_timer) as Timer;

            Assert.That(timer.Interval, Is.EqualTo(expected));
            Assert.True(timer.Enabled);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-1000)]
        public void IntervalNegativeTest(int interval)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _timer.Start(interval);
            });
        }

        [Test]
        public void TimerStopTest()
        {
            _timer.Start(5);
            _timer.Elapsed += (s, e) => { SomeMethodToTest(); };
            _timer.Stop();

            var field = typeof(TimerController).GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
            var timer = field?.GetValue(_timer) as Timer;

            Assert.IsFalse(timer.Enabled);
            Assert.False(_wasInvoked);
        }

        [Test]
        public void TimerDisposeTest()
        {
            _timer.Start(5);
            _timer.Elapsed += (s, e) => { SomeMethodToTest(); };
            _timer.Dispose();

            var field = typeof(TimerController).GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
            var timer = field?.GetValue(_timer) as Timer;

            Assert.IsNull (timer);
            Assert.False(_wasInvoked);
        }

        [Test]
        public async Task TimerElapseTest()
        {
            typeof(ITimer).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_timer, new Timer());
            _timer.Elapsed += (s, e) => SomeMethodToTest();
            _timer.Start(1);
            await Task.Delay(1500);

            Assert.IsTrue(_wasInvoked);
        }

        private bool _wasInvoked = false;
        private void SomeMethodToTest()
        {
            _wasInvoked = true;
        }
    }
}