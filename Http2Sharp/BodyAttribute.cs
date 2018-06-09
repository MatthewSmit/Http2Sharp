using System;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Http2Sharp
{
    /// <summary>
    /// Binds the body of the HttpRequest to a parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class BodyAttribute : BindingAttribute
    {
        private sealed class BodyBinding : Binding
        {
            public BodyBinding([NotNull] Type type)
                : base(type)
            {
            }

            /// <inheritdoc />
            public override object Bind(HttpRequest request)
            {
                if (request.ContentType != null)
                {
                    if (request.ContentType.StartsWith("text/", StringComparison.InvariantCulture))
                    {
                        // TODO: Encoding
                        return Encoding.UTF8.GetString(request.Body);
                    }

                    switch (request.ContentType)
                    {
                        case "application/json":
                            // TODO: Encoding
                            var text = Encoding.UTF8.GetString(request.Body);
                            return JsonConvert.DeserializeObject(text, ParameterType);
                        default:
                            throw new NotImplementedException();
                    }
                }

                return ConvertType(request.Body);
            }
        }

        /// <inheritdoc />
        internal override Binding CreateBinding(ParameterInfo parameterInfo)
        {
            return new BodyBinding(parameterInfo.ParameterType);
        }
    }
}
