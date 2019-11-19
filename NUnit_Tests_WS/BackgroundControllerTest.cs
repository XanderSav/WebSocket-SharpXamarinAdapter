using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using WebSocketSharpXamarinAdapter.BackgroundHandler;
using WebSocketSharpXamarinAdapter.ConnectionHandler;

namespace NUnit_Tests_WS.WebSocketTest
{
    [TestFixture]
    public class BackgroundControllerTest
    {
        private IBackgroundController _backgroundController;
        private Mock<ISocketConnectionController> _connectionControllerMock;

        [SetUp]
        public void SetUp()
        {
            _connectionControllerMock = new Mock<ISocketConnectionController>(MockBehavior.Strict);
            _backgroundController = new BackgroundController(_connectionControllerMock.Object);
            _isPassed = false;
        }

        [Test]
        public void CtorNullTest()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundController(null));
        }

        [Test]
        public void InitTest()
        {
            _backgroundController.Init(() => MethodToTest());
            var action = typeof(BackgroundController).GetField("SocketClose", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_backgroundController) as Action;

            Assert.IsNotNull(action);
        }

        [Test]
        public async Task EnteredBackgroundSocketClosedTest()
        {
            typeof(BackgroundController).GetField("BackgroundInterval", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, (ushort)1000);
            typeof(BackgroundController).GetField("SocketClose", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, new Action(MethodToTest));

            await _backgroundController.EnteredBackground();

            Assert.IsTrue(_isPassed);
        }

        [Test]
        public async Task EnteredBackgroundCanceledTest()
        {
            typeof(BackgroundController).GetField("BackgroundInterval", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, (ushort)3000);
            typeof(BackgroundController).GetField("SocketClose", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, new Action(MethodToTest));
            var task = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                var cts = typeof(BackgroundController).GetField("_backgroundCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_backgroundController) as CancellationTokenSource;
                cts?.Cancel();
            });
            await _backgroundController.EnteredBackground();

            Assert.IsFalse(_isPassed);
        }

        [Test]
        public async Task EnteredForegroundBeforeSocketWasClosedTest()
        {
            _connectionControllerMock.Setup(c => c.Connect()).Returns(Task.FromResult(true));
            typeof(BackgroundController).GetField("_delayTask", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, new Task(MethodToTest));
            var t = typeof(BackgroundController).GetField("_delayTask", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_backgroundController) as Task;
            typeof(BackgroundController).GetField("_backgroundCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, new CancellationTokenSource());
            var cts = typeof(BackgroundController).GetField("_backgroundCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_backgroundController) as CancellationTokenSource;

            await _backgroundController.EnteredForeground();
            
            Assert.False(_isPassed);
            Assert.IsTrue(cts.Token.IsCancellationRequested);
            _connectionControllerMock.Verify(c => c.Connect(), Times.Never);
        }

        [Test]
        public async Task EnteredForegroundAfterSocketWasClosedTest()
        {
            _connectionControllerMock.Setup(c => c.Connect()).Returns(Task.FromResult(true));
            typeof(BackgroundController).GetField("_delayTask", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, new Task(MethodToTest));
            var t = typeof(BackgroundController).GetField("_delayTask", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_backgroundController) as Task;
            typeof(BackgroundController).GetField("_backgroundCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_backgroundController, new CancellationTokenSource());
            var cts = typeof(BackgroundController).GetField("_backgroundCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_backgroundController) as CancellationTokenSource;
            t.Start();
            await _backgroundController.EnteredForeground();
            Assert.IsTrue(_isPassed);
            Assert.False(cts.Token.IsCancellationRequested);
            _connectionControllerMock.Verify(c => c.Connect(), Times.Once);
        }

        bool _isPassed = false;
        public void MethodToTest()
        {
            _isPassed = true;
        }
    }
}