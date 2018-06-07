using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class BindingAttribute : Attribute
    {
        [NotNull] internal abstract Binding CreateBinding([NotNull] ParameterInfo parameterInfo);
    }
}