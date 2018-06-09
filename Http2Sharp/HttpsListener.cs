using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class HttpsListener : HttpListener
    {
        [NotNull] private readonly X509Certificate serverCertificate;

        /// <inheritdoc />
        public HttpsListener([NotNull] IPAddress address, int port, [NotNull] X509Certificate certificate)
            : base(address, port)
        {
            serverCertificate = certificate;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            serverCertificate.Dispose();
            base.Dispose();
        }

        /// <inheritdoc />
        protected override IHttpClient CreateClient(TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var stream = new SslStream(client.GetStream());
            stream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls12, true);
            return new HttpClient(client, stream);
        }
    }
}
