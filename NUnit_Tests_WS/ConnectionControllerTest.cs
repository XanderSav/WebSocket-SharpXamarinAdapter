using System;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WebSocketSharpXamarinAdapter.ConnectionHandler;
using WebSocketSharpXamarinAdapter.DTO;
using WebSocketSharpXamarinAdapter.ReconnectionControllers;
using WebSocketSharpXamarinAdapter.WebSocket;

namespace NUnit_Tests_WS.WebSocketTest
{
    [TestFixture]
    public class SocketConnectionControllerTest
    {
        private ISocketConnectionController _socketConnectionController;
        private Mock<IWebSocketImplementation> _socketMock;
        private Mock<ITimer> _timerMock;

        [SetUp]
        public void SetUp()
        {
            _socketConnectionController = new SocketConnectionController();
            _socketMock = new Mock<IWebSocketImplementation>(MockBehavior.Strict);
            _timerMock = new Mock<ITimer>(MockBehavior.Strict);
            _isPassed = false;
        }

        private bool _isPassed;
        public void MethodToTest()
        {
            _isPassed = true;
        }

        [Test]
        public void InitTest()
        {
            _socketConnectionController.Init();
            var socket = typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController) as WebSocketImplementation;
            var recoveryTimer = typeof(SocketConnectionController).GetField("_reopenTimer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController) as ITimer;

            Assert.IsNotNull(socket);
            Assert.IsNotNull(recoveryTimer);
        }

        [Test]
        public void ConnectTest()
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(s => s.Init(It.IsAny<SocketParameters>()));
            _socketMock.Setup(s => s.SocketState).Returns(WebSocketSharp.WebSocketState.Closed);
            _socketMock.Setup(s => s.Open()).Returns(Task.FromResult(true));
            _socketConnectionController.NeedConnection += tcs =>
            {
                _isPassed = true;
                tcs.SetResult(new SocketParameters("", "", "", "", "","", ""));
            };

            _socketConnectionController.Connect();
            var cts = typeof(SocketConnectionController).GetField("_socketParametersCts", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController) as TaskCompletionSource<SocketParameters>;
            var param = typeof(SocketConnectionController).GetField("_socketParameters", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController) as SocketParameters;

            Assert.IsNotNull(cts, "cts");
            Assert.IsNotNull(param, "param");
            Assert.IsTrue(_isPassed);

            _socketMock.Verify(s => s.Init(It.IsAny<SocketParameters>()), Times.Once);
            _socketMock.Verify(s => s.Open(), Times.Once);
        }

        [Test]
        public void ConnectExceptionTest()
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(s => s.Init(It.IsAny<SocketParameters>()));
            _socketMock.Setup(s => s.SocketState).Returns(WebSocketSharp.WebSocketState.Closed);
            _socketMock.Setup(s => s.Open()).Throws(new Exception());
            _socketConnectionController.NeedConnection += tcs =>
            {
                _isPassed = true;
                tcs.SetResult(new SocketParameters("", "", "", "","", "", ""));
            };

            try
            {
                _socketConnectionController.Connect();
            }
            catch (Exception e)
            {
                Assert.That(e is Exception);
            }
            var cts = typeof(SocketConnectionController).GetField("_socketParametersCts", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController) as TaskCompletionSource<SocketParameters>;
            var param = typeof(SocketConnectionController).GetField("_socketParameters", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController) as SocketParameters;

            Assert.IsNotNull(cts, "cts");
            Assert.IsNotNull(param, "param");
            Assert.IsTrue(_isPassed);


            _socketMock.Verify(s => s.Init(It.IsAny<SocketParameters>()), Times.Once);
            _socketMock.Verify(s => s.Open(), Times.Once);
        }

        [Test]
        public void CloseByBgdPositiveTest()
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(f => f.SocketState).Returns(WebSocketSharp.WebSocketState.Open);
            _socketMock.Setup(s => s.Close(false));

            _socketConnectionController.CloseByBgd();

            _socketMock.Verify(s => s.Close(false), Times.Once);
        }

        [TestCase(WebSocketSharp.WebSocketState.Closed)]
        [TestCase(WebSocketSharp.WebSocketState.Closing)]
        [TestCase(WebSocketSharp.WebSocketState.Connecting)]
        public void CloseByBgdNegativeTest(WebSocketSharp.WebSocketState state)
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(f => f.SocketState).Returns(state);
            _socketMock.Setup(s => s.Close(false));

            _socketConnectionController.CloseByBgd();

            _socketMock.Verify(s => s.Close(false), Times.Never);
        }

        [TestCase(null)]
        [TestCase(true)]
        [TestCase(false)]
        public void CloseTest(bool? isBackground)
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(s => s.Close(isBackground));

            _socketConnectionController.Close(isBackground);

            _socketMock.Verify(s => s.Close(isBackground), Times.Once);
        }

        [Test]
        public void SendTest()
        {
            var data = new JObject();
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(s => s.Send(data));

            _socketConnectionController.Send(data);

            _socketMock.Verify(s => s.Send(data), Times.Once);
        }

        [Test]
        public void ReopenTimerElapsedTest()
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(s => s.Init(It.IsAny<SocketParameters>()));
            _socketMock.Setup(s => s.SocketState).Returns(WebSocketSharp.WebSocketState.Closed);
            _socketMock.Setup(s => s.Open()).Returns(Task.FromResult(true));
            _socketConnectionController.NeedConnection += tcs =>
            {
                _isPassed = true;
                tcs.SetResult(new SocketParameters("", "", "", "","", "", ""));
            };
            typeof(SocketConnectionController).GetField("_reopenTimer", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _timerMock.Object);
            var inretval = (ushort)typeof(SocketConnectionController).GetField("ReopenInterval", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController);
            _timerMock.Setup(timer => timer.Stop());
            _timerMock.Setup(timer => timer.Start(inretval));

            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("_reopenTimer_Elapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_socketConnectionController, new object[] { new object(), null });

            _timerMock.Verify(timer => timer.Stop(), Times.Once);
            _timerMock.Verify(timer => timer.Start(inretval), Times.Never);
        }

        [Test]
        public void ReopenTimerElapsedNegativeTest()
        {
            typeof(SocketConnectionController).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _socketMock.Object);
            _socketMock.Setup(s => s.Init(It.IsAny<SocketParameters>()));
            _socketMock.Setup(s => s.SocketState).Returns(WebSocketSharp.WebSocketState.Closed);
            _socketMock.Setup(s => s.Open()).Returns(Task.FromResult(false));
            _socketConnectionController.NeedConnection += tcs =>
            {
                _isPassed = true;
                tcs.SetResult(new SocketParameters("", "", "","", "", "", ""));
            };
            typeof(SocketConnectionController).GetField("_reopenTimer", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _timerMock.Object);
            var inretval = (ushort)typeof(SocketConnectionController).GetField("ReopenInterval", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_socketConnectionController);
            _timerMock.Setup(timer => timer.Stop());
            _timerMock.Setup(timer => timer.Start(inretval));

            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("_reopenTimer_Elapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_socketConnectionController, new object[] { new object(), null });

            _timerMock.Verify(timer => timer.Stop(), Times.Once);
            _timerMock.Verify(timer => timer.Start(inretval), Times.Once);
        }

        [Test]
        public void SocketClosedBGDTest()
        {
            _socketConnectionController.SocketReadyForReconnect += MethodToTest;
            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("SocketClosed", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_socketConnectionController, new object[] { DisconnectedReason.BGD });
            Assert.IsTrue(_isPassed);
        }

        [Test]
        public void SocketClosedUserTest()
        {
            _socketConnectionController.SocketClosedByUser += MethodToTest;
            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("SocketClosed", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_socketConnectionController, new object[] { DisconnectedReason.User });
            Assert.IsTrue(_isPassed);
        }

        [Test]
        public void SocketClosedUnknownTest()
        {
            typeof(SocketConnectionController).GetField("_reopenTimer", BindingFlags.Instance | BindingFlags.NonPublic)?.
                SetValue(_socketConnectionController, _timerMock.Object);
            _timerMock.Setup(timer => timer.Start(5));
            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("SocketClosed", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_socketConnectionController, new object[] { DisconnectedReason.Unknown });
            _timerMock.Verify(timer => timer.Start(5), Times.Once);
        }

        [Test]
        public void SocketClosedExceptionTest()
        {
            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("SocketClosed", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                method?.Invoke(_socketConnectionController, new object[] {100});
            }
            catch (Exception e)
            {
                Assert.That(e.InnerException is ArgumentOutOfRangeException);
            }
        }

        [Test]
        public void Socket_OnMessageTest()
        {
            string res = ""; 
            string exp = "mock"; 
            _socketConnectionController.OnMessage += f => { MethodToTest(); res = f; };
            MethodInfo method = typeof(SocketConnectionController).
                GetMethod("Socket_OnMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_socketConnectionController, new object[] { exp });

            Assert.IsTrue(_isPassed);
            Assert.AreEqual(exp, res);
        }
    }
}