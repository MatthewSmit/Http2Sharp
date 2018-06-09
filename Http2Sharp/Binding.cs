using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// Binds an element of the HttpRequest to an object to be passed along a route.
    /// </summary>
    internal abstract class Binding
    {
        /// <summary>
        /// The required type of the object.
        /// </summary>
        [NotNull] private readonly Type type;

        protected Binding([NotNull] Type type)
        {
            this.type = type;
        }

        public abstract object Bind([NotNull] HttpRequest request);

        protected object ConvertType(object value)
        {
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        [NotNull]
        protected Type ParameterType => type;
    }
}
