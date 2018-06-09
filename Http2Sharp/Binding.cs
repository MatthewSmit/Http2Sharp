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
        [NotNull] private readonly Type type;

        protected Binding([NotNull] Type type)
        {
            this.type = type;
        }

        /// <summary>
        /// Gets the required information from the HttpRequest and returns an object.
        /// </summary>
        public abstract object Bind([NotNull] HttpRequest request);

        protected object ConvertType(object value)
        {
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The required type of the object.
        /// </summary>
        [NotNull]
        protected Type ParameterType => type;
    }
}
