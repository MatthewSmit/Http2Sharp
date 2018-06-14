using System;
using System.IO;
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
                if (ParameterType == typeof(Stream))
                {
                    return request.GetBodyStream();
                }

                if (ParameterType == typeof(byte[]))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        request.GetBodyStream().CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }

                if (request.ContentType != null)
                {
                    if (request.ContentType.StartsWith("text/", StringComparison.InvariantCulture))
                    {
                        // TODO: Encoding
                        using (var stream = request.GetBodyStream())
                        {
                            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }

                    switch (request.ContentType)
                    {
                        case "application/json":
                            // TODO: Encoding
                            using (var stream = request.GetBodyStream())
                            {
                                using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    var text = streamReader.ReadToEnd();
                                    return JsonConvert.DeserializeObject(text, ParameterType);
                                }
                            }

                        default:
                            throw new NotImplementedException();
                    }
                }
//
//                return ConvertType(request.Body);
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        internal override Binding CreateBinding(ParameterInfo parameterInfo)
        {
            return new BodyBinding(parameterInfo.ParameterType);
        }
    }
}
