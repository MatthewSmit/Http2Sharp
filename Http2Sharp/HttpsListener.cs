using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class HttpsListener : HttpListener
    {
        [NotNull] private readonly X509Certificate serverCertificate;

        /// <inheritdoc />
        public HttpsListener([NotNull] IServerConfiguration serverConfiguration, [NotNull] IPAddress address, int port, [NotNull] X509Certificate certificate)
            : base(serverConfiguration, address, port)
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
        protected override async Task<HttpStream> CreateClientStreamAsync(TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var stream = new SslStream(client.GetStream());
            var serverOptions = new SslServerAuthenticationOptions
            {
                AllowRenegotiation = true,
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = false,
                EnabledSslProtocols = SslProtocols.Tls12,
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    SslApplicationProtocol.Http2,
                    SslApplicationProtocol.Http11,
                },
                CertificateRevocationCheckMode = X509RevocationMode.Offline,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption
            };

            await stream.AuthenticateAsServerAsync(serverOptions, CancellationToken.None).ConfigureAwait(false);
            return new HttpStream(stream);
        }

        /// <inheritdoc />
        public override string Protocol => "https";
    }
}
