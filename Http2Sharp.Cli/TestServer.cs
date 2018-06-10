using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Http2Sharp.Test;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Http2Sharp.Cli
{
    [Router]
    internal sealed class TestServer
    {
        private readonly IList<User> users;

        public TestServer()
        {
            users = JsonConvert.DeserializeObject<IList<User>>(File.ReadAllText("userData.json"));
        }

        [NotNull]
        [Get("/")]
        public HttpResponse MainPage()
        {
            return HttpResponse.Send("Hello World");
        }

        [NotNull]
        [Get("/favicon.ico")]
        public HttpResponse FavIcon()
        {
            return HttpResponse.SendFile("data/favicon.ico");
        }

        [NotNull]
        [Get("/hello")]
        public HttpResponse Hello([Query(Default = "Bob")] string name)
        {
            return HttpResponse.Send("Hello " + name);
        }

        [NotNull]
        [Get("/user/{id:\\d+}")]
        public HttpResponse User([Param] int id)
        {
            if (id < 0 || id >= users.Count)
            {
                return HttpResponse.Status(HttpStatusCode.BadRequest);
            }
            return HttpResponse.Send(users[id]);
        }

        [NotNull]
        [Post("/user/{id:\\d+}")]
        public HttpResponse PostUser([Param] int id, [Body] User user)
        {
            if (id < 0 || id >= users.Count)
            {
                return HttpResponse.Status(HttpStatusCode.BadRequest);
            }

            users[id] = user;
            return HttpResponse.Send(users[id]);
        }

        [Get("/exception")]
        public HttpResponse Exception()
        {
            throw new NotSupportedException("This is a test message");
        }
    }
}