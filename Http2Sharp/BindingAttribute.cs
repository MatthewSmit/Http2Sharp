using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// Base class to handle binding an element from HttpRequest to a parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class BindingAttribute : Attribute
    {
        /// <summary>
        /// Creates a binding class for this attribute.
        /// </summary>
        [NotNull] internal abstract Binding CreateBinding([NotNull] ParameterInfo parameterInfo);
    }
}
