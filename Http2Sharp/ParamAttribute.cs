using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ParamAttribute : BindingAttribute
    {
        private sealed class ParamBinding : Binding
        {
            [NotNull] private readonly string name;

            public ParamBinding([NotNull] Type type, [NotNull] string name)
                : base(type)
            {
                this.name = name;
            }

            /// <inheritdoc />
            public override object Bind(HttpRequest request)
            {
                return ConvertType(request.Parameters[name]);
            }
        }

        /// <inheritdoc />
        internal override Binding CreateBinding(ParameterInfo parameterInfo)
        {
            var name = Name ?? parameterInfo.Name;
            return new ParamBinding(parameterInfo.ParameterType, name);
        }

        public string Name { get; set; }
    }
}
