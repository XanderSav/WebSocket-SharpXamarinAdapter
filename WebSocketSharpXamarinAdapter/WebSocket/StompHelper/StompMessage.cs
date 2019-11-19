using System.Collections.Generic;

namespace WebSocketSharpXamarinAdapter.WebSocket.StompHelper
{
    public class StompMessage
    {
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StompMessage" /> class.
        /// </summary>
        /// <param name = "command">The command.</param>
        public StompMessage(string command)
            : this(command, string.Empty)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StompMessage" /> class.
        /// </summary>
        /// <param name = "command">The command.</param>
        /// <param name = "body">The body.</param>
        public StompMessage(string command, string body)
            : this(command, body, new Dictionary<string, string>())
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StompMessage" /> class.
        /// </summary>
        /// <param name = "command">The command.</param>
        /// <param name = "body">The body.</param>
        /// <param name = "headers">The headers.</param>
        internal StompMessage(string command, string body, Dictionary<string, string> headers)
        {
            Command = command;
            Body = body;
            _headers = headers;

            this["content-length"] = body.Length.ToString();
        }

        public Dictionary<string, string> Headers => _headers;

        /// <summary>
        /// Gets the body.
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// Gets the command.
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// Gets or sets the specified header attribute.
        /// </summary>
        public string this[string header]
        {
            get => _headers.ContainsKey(header) && _headers[header] != null ? _headers[header] : string.Empty;
            set => _headers[header] = value;
        }

        public override bool Equals(object obj)
        {
            var message = obj as StompMessage;
            return message != null &&
                   Body == message.Body &&
                   Command == message.Command;
        }

        public override int GetHashCode()
        {
            var hashCode = 2078648008;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Body);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Command);
            return hashCode;
        }
    }
}
