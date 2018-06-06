using System;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ParamAttribute : Attribute
    {
        public ParamAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}