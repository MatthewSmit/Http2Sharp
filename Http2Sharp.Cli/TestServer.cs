using System;
using System.Collections.Generic;
using System.IO;
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
        [Get("/hello")]
        public HttpResponse Hello([Query(Default = "Bob")] string name)
        {
            return HttpResponse.Send("Hello " + name);
        }

        [Get("/user/{id:\\d+}")]
        public HttpResponse User([Param] int id)
        {
            if (id < 0 || id >= users.Count)
            {
                return HttpResponse.Status(400);
            }
            return HttpResponse.Send(users[id]);
        }

        [Post("/user/{id:\\d+}")]
        public HttpResponse PostUser([Param] int id, [Body] User user)
        {
            if (id < 0 || id >= users.Count)
            {
                return HttpResponse.Status(400);
            }

            users[id] = user;
            return HttpResponse.Send(users[id]);
        }

        [Get("/exception")]
        public HttpResponse Exception()
        {
            throw new NotImplementedException("This is a test message");
        }
    }
}