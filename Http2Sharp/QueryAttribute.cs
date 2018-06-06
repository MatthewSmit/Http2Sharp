using System;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryAttribute : Attribute
    {
        public QueryAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        //TODO: Change from string
        public string Default { get; set; }
    }
}