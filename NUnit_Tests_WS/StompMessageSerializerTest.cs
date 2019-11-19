using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Text.RegularExpressions;
using WebSocketSharpXamarinAdapter.WebSocket.StompHelper;

namespace NUnit_Tests_WS.WebSocketTest
{
    [TestFixture]
    public class StompMessageSerializerTest
    {
        private IStompMessageSerializer _messageSerializer;

        [SetUp]
        public void Setup()
        {
            _messageSerializer = new StompMessageSerializer();
        }

        [Test]
        public void SerializeTest()
        {
            var jObj = new JObject
            {
                ["sid"] = "SessionId",
                ["umid"] = "UmId",
                ["cmd"] = "Command"
            };
            var message = new StompMessage(StompFrame.SEND, jObj.ToString())
            {
                ["Mockdata"] = null,
                ["destination"] = "/exchange/CMD"
            };
            var actual = _messageSerializer.Serialize(message);
            var Exp = @"SEND\ncontent-length:66\nMockdata:\ndestination:/exchange/CMD\n\n{\r\n  \""sid\"": \""SessionId\"",\r\n  \""umid\"": \""UmId\"",\r\n  \""cmd\"": \""Command\""\r\n}\0";
            Assert.That(actual, Is.EqualTo(Regex.Unescape(Exp)));
        }

        [Test]
        public void SerializeNullMessageTest()
        {
            StompMessage message = null;
            var actual = _messageSerializer.Serialize(message);
            Assert.That(actual, Is.EqualTo(null));
        }

        [Test]
        public void DeserializeTest()
        {
            var jObj = new JObject
            {
                ["sid"] = "SessionId",
                ["umid"] = "UmId",
                ["cmd"] = "Command"
            };
            var expected = new StompMessage(StompFrame.SEND, jObj.ToString())
            {
                ["destination"] = "/exchange/CMD"
            };
            var serializedMessage =
                @"SEND\ncontent-length:66\ndestination:/exchange/CMD\n\n{\r\n  \""sid\"": \""SessionId\"",\r\n  \""umid\"": \""UmId\"",\r\n  \""cmd\"": \""Command\""\r\n}\0";
            var actual = _messageSerializer.Deserialize(Regex.Unescape(serializedMessage));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Headers, Is.EquivalentTo(expected.Headers));
        }

        [Test]
        public void DeserializeNullTest()
        {
            var actual = _messageSerializer.Deserialize(null);
            Assert.That(actual, Is.EqualTo(null));
        }

        [Test]
        public void DeserializeEmptyTest()
        {
            var actual = _messageSerializer.Deserialize("");
            Assert.That(actual, Is.EqualTo(null));
        }
    }
}