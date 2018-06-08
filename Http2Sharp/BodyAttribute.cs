using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
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
            public override object Bind(IReadOnlyDictionary<string, string> parameters, IReadOnlyList<(string, string)> queries, object body)
            {
                return ConvertType(body);
            }
        }

        /// <inheritdoc />
        internal override Binding CreateBinding(ParameterInfo parameterInfo)
        {
            return new BodyBinding(parameterInfo.ParameterType);
        }
    }
}