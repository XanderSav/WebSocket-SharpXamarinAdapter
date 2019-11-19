using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading.Tasks;
using WebSocketSharpXamarinAdapter.DTO;
using WebSocketSharpXamarinAdapter.ReconnectionControllers;
using WebSocketSharpXamarinAdapter.WebSocket;
using WebSocketSharpXamarinAdapter.WebSocket.StompHelper;

namespace NUnit_Tests_WS.WebSocketTest
{
    [TestFixture]
    public class WebSocketImplementationTest
    {
        private WebSocketImplementation _webSocket;
        private Mock<IWebSocket> _webSocketMock;
        private Mock<IStompMessageSerializer> _stompMessageSerializer;

        [SetUp]
        public void Setup()
        {
            _webSocket = new WebSocketImplementation();
            _webSocketMock = new Mock<IWebSocket>(MockBehavior.Strict);
            _stompMessageSerializer = new Mock<IStompMessageSerializer>(MockBehavior.Strict);
        }

        [TestCase("stompUser", "stompPsw", "session", "umId","host", "domain", "withoutNormalValue")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId","some.internet.address", "domain", "123")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "host", ".domain.classifier", "123")]
        [TestCase("Вася", "Пупкин", "session", "umId", "host", "domain", "2")]
        public void InitTest(string stompUser, string stompPsw, string session, string umId, string host, string domain, string webSrv)
        {
            var exp = new SocketParameters(
                stompUser,
                stompPsw,
                "session." + session,
                umId,
                host,
                domain,
                webSrv);
            _webSocket.Init(exp);
            var socketParameters = _webSocket.GetType().GetField("_socketParameters", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_webSocket) as SocketParameters;
            var socket = _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_webSocket) as IWebSocket;
            Assert.That(socketParameters, Is.EqualTo(exp));
            Assert.That(socket, Is.Not.Null);
        }

        [Test]
        public void InitFailTest()
        {
            Assert.Throws<ArgumentNullException>(() => _webSocket.Init(null));
        }

        [TestCase("stompUser", "stompPsw", "session", "umId", "host", "domain", "withoutNormalValue")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "some.internet.address", "domain", "123")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "host", ".domain.classifier", "123")]
        [TestCase("Вася", "Пупкин", "session", "umId", "host", "domain", "2")]
        public void WebSocketOpenTest(string stompUser, string stompPsw, string session, string umId, string host, string domain, string webSrv)
        {
            var exp = new SocketParameters(
                stompUser,
                stompPsw,
                "session." + session,
                umId,
                host,
                domain,
                webSrv);
            var cookie = new WebSocketSharp.Net.Cookie("WEBSRV", webSrv, "/", domain);
            _webSocketMock.Setup(w => w.ConnectAsync());
            _webSocketMock.Setup(w => w.SetCookie(cookie));
            _webSocketMock.Setup(w => w.Subscribe());
            _webSocket.GetType().GetField("_socketParameters", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, exp);
            _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, _webSocketMock.Object);
            _webSocket.GetType().GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, new TimerController());
            _webSocket.GetType().GetField("_stompConnectedCompletionSource", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, new TaskCompletionSource<bool>());
            _webSocket.Open();

            var tcs = _webSocket.GetType().GetField("_stompConnectedCompletionSource", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_webSocket) as TaskCompletionSource<bool>;
            tcs.TrySetResult(true);
            var socket = _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_webSocket) as IWebSocket;

            Assert.That(socket, Is.Not.Null);
            _webSocketMock.Verify(w => w.ConnectAsync(), Times.Once);
            _webSocketMock.Verify(w => w.SetCookie(cookie), Times.Once);
        }

        [Test]
        public void CloseTest()
        {
            _webSocketMock.Setup(w => w.Unsubscribe());
            _webSocket.Close(true);

            var socket = _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_webSocket) as IWebSocket;
            Assert.That(socket, Is.Null);
        }

        [TestCase("stompUser", "stompPsw", "session", "umId", "host", "domain", "withoutNormalValue")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "some.internet.address", "domain", "123")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "host", ".domain.classifier", "123")]
        [TestCase("Вася", "Пупкин", "session", "umId", "host", "domain", "2")]
        public void ConnectStompTest(string stompUser, string stompPsw, string session, string umId, string host, string domain, string webSrv)
        {
            _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, _webSocketMock.Object);
            var exp = new SocketParameters(
                stompUser,
                stompPsw,
                "session." + session,
                umId,
                host,
                domain,
                webSrv);
            _webSocket.GetType().GetField("_socketParameters", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, exp);
            var connectMsg = new StompMessage(StompFrame.CONNECT)
            {
                ["login"] = exp.StompUser,
                ["passcode"] = exp.StompPassword,
                ["host"] = exp.Host,
                ["accept-version"] = "1.2",
                ["heart-beat"] = "0,5000"
            };
            var expValue =
                $"{StompFrame.CONNECT}\ncontent-length:0\nlogin:{stompUser}\npasscode:{stompPsw}\nhost:trading\naccept-version:1.2\nheart-beat:0,5000\n\n\0";

            _stompMessageSerializer.Setup(s => s.Serialize(connectMsg)).Returns(expValue);
            _webSocketMock.Setup(s => s.Send($"[\"{_stompMessageSerializer.Object.Serialize(connectMsg)}\"]"));
            MethodInfo method = _webSocket.GetType().
                GetMethod("ConnectStomp", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_webSocket, new object[] { });
            _webSocketMock.Verify(w => w.Send($"[\"{expValue}\"]"), Times.Once);
            _stompMessageSerializer.Verify(s => s.Serialize(connectMsg), Times.AtMost(2));
        }

        [TestCase("stompUser", "stompPsw", "session", "umId", "host", "domain", "withoutNormalValue")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "some.internet.address", "domain", "123")]
        [TestCase("lkjhkljh", "4564ghf", "session", "umId", "host", ".domain.classifier", "123")]
        [TestCase("Вася", "Пупкин", "session", "umId", "host", "domain", "2")]
        public void SubscribeStompTest(string stompUser, string stompPsw, string session, string umId, string host, string domain, string webSrv)
        {
            _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, _webSocketMock.Object);
            var exp = new SocketParameters(
                stompUser,
                stompPsw,
                session,
                umId,
                host,
                domain,
                webSrv);
            _webSocket.GetType().GetField("_socketParameters", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, exp);
            var subscribeMsg = new StompMessage(StompFrame.SUBSCRIBE)
            {
                ["id"] = "sub-0",
                ["destination"] = $"/amq/queue/{exp.Session}"
            };
            var expValue =
                $"{StompFrame.SUBSCRIBE}\ncontent-length:0\nid:sub-0\ndestination:/amq/queue/{"session." + session}\n\n\0";
            _stompMessageSerializer.Setup(s => s.Serialize(subscribeMsg)).Returns(expValue);
            _webSocketMock.Setup(s => s.Send($"[\"{_stompMessageSerializer.Object.Serialize(subscribeMsg)}\"]"));

            MethodInfo method = _webSocket.GetType().
                GetMethod("SubscribeStomp", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_webSocket, new object[] { });

            _webSocketMock.Verify(w => w.Send($"[\"{expValue}\"]"), Times.Once);
            _stompMessageSerializer.Verify(s => s.Serialize(subscribeMsg), Times.AtMost(2));
        }

        [Test]
        public void SendBehaviourTest()
        {
            var data = "[\r\n  \"SEND\\ncontent-length:2\\ndestination:/exchange/CMD/\\n\\n{}\\u0000\"\r\n]";
            _webSocketMock.Setup(w => w.Send(data));
            _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, _webSocketMock.Object);
            _webSocket.Send(new JObject());

            _webSocketMock.Verify(w => w.Send(data), Times.Once);
        }

        [Test]
        public void SendDataTest()
        {
            var jObj = new JObject
            {
                ["sid"] = "SessionId",
                ["umid"] = "UmId",
                ["cmd"] = "Command"
            };
            var data =
                "SEND\ncontent-length:66\ndestination:/exchange/CMD/\n\n{\r\n  \"sid\": \"SessionId\",\r\n  \"umid\": \"UmId\",\r\n  \"cmd\": \"Command\"\r\n}\0";
            var getSettingsMsg = new StompMessage(StompFrame.SEND, jObj.ToString())
            {
                ["destination"] = "/exchange/CMD/"
            };
            _webSocket.GetType().GetField("_webSocket", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_webSocket, _webSocketMock.Object);
            _stompMessageSerializer.Setup(s => s.Serialize(getSettingsMsg)).Returns(data);
            _webSocketMock.Setup(w => w.Send(new JArray(_stompMessageSerializer.Object.Serialize(getSettingsMsg)).ToString()));
            _webSocket.Send(jObj);

            _webSocketMock.Verify(w => w.Send(new JArray(data).ToString()), Times.Once);
            _stompMessageSerializer.Verify(s => s.Serialize(getSettingsMsg), Times.AtMost(2));
        }
    }
}