using System;

namespace Http2Sharp.Test
{
    public sealed class User
    {
        public Gender Gender { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Username { get; set; }

        public DateTime DateOfBirth { get; set; }
    }
}