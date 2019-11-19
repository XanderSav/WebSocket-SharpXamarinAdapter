using System;
using System.Collections.Generic;

namespace WebSocketSharpXamarinAdapter.DTO
{
    public class SocketParameters
    {
        public SocketParameters(string stompUser, string stompPassword, string session, string umId, string host, string domain, string webSrv)
        {
            StompUser = stompUser ?? throw new ArgumentNullException(nameof(stompUser));
            StompPassword = stompPassword ?? throw new ArgumentNullException(nameof(stompPassword));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            UmId = umId ?? throw new ArgumentNullException(nameof(umId));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            WebSrv = webSrv;
        }

        public string StompUser { get; set; }
        public string StompPassword { get; set; }
        public string Session { get; set; }
        public string UmId { get; set; }
        public string Host { get; set; }
        public string Domain { get; set; }
        public string WebSrv { get; set; }

        public override bool Equals(object obj)
        {
            var parameters = obj as SocketParameters;
            return parameters != null &&
                   StompUser == parameters.StompUser &&
                   StompPassword == parameters.StompPassword &&
                   Session == parameters.Session &&
                   UmId == parameters.UmId &&
                   Host == parameters.Host &&
                   Domain == parameters.Domain &&
                   WebSrv == parameters.WebSrv;
        }

        public override int GetHashCode()
        {
            var hashCode = 173017603;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(StompUser);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(StompPassword);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Session);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UmId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Host);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Domain);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(WebSrv);
            return hashCode;
        }
    }
}
