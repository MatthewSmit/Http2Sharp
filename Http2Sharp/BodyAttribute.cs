using System;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class BodyAttribute : Attribute
    {
    }
}