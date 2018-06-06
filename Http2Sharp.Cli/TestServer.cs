using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Http2Sharp.Cli
{
    [Router]
    [UsedImplicitly]
    internal sealed class TestServer
    {
        private readonly IList<User> users = new List<User>();

        public TestServer()
        {
            throw new NotImplementedException();
        }

        [Get("/")]
        public HttpResponse MainPage()
        {
            return HttpResponse.Send("Hello World");
        }

        [Get("/hello")]
        public HttpResponse Hello([Query("name", Default = "Bob")] string name)
        {
            return HttpResponse.Send("Hello " + name);
        }

        [Get("/user/{id}")]
        public HttpResponse User([Param("id")] int id)
        {
            if (id < 0 || id >= users.Count)
            {
                return HttpResponse.Status(400);
            }
            return HttpResponse.Send(users[id]);
        }

        [Post("/user/{id}")]
        public HttpResponse PostUser([Param("id")] int id, [Body] User user)
        {
            if (id < 0 || id >= users.Count)
            {
                return HttpResponse.Status(400);
            }

            users[id] = user;
            return HttpResponse.Send(users[id]);
        }

        [Post("/exception")]
        public HttpResponse Exception()
        {
            throw new NotImplementedException("This is a test message");
        }
    }
}