﻿using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Http2Sharp.Cli
{
    internal static class Program
    {
        private static void Main()
        {
            using (var server = new HttpServer(new TestServer()))
            {
                server.AddListener(new HttpListener(server, IPAddress.Any, 8080));
                server.AddListener(new HttpsListener(server, IPAddress.Any, 8081, new X509Certificate2("key.pfx")));
                server.StartListen();
                server.WaitForAll();
            }
        }
    }
}
