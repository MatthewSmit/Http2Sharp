using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryAttribute : BindingAttribute
    {
        private sealed class QueryBinding : Binding
        {
            [NotNull] private readonly string name;
            [CanBeNull] private readonly string defaultValue;

            public QueryBinding([NotNull] Type type, [NotNull] string name, [CanBeNull] string defaultValue)
                : base(type)
            {
                this.name = name;
                this.defaultValue = defaultValue;
            }

            /// <inheritdoc />
            public override object Bind(IReadOnlyDictionary<string, string> parameters, IReadOnlyList<(string, string)> queries, object body)
            {
                foreach (var (queryName, queryValue) in queries)
                {
                    if (string.Equals(queryName, name))
                    {
                        return ConvertType(queryValue);
                    }
                }

                if (defaultValue != null)
                {
                    return ConvertType(defaultValue);
                }

                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        internal override Binding CreateBinding(ParameterInfo parameterInfo)
        {
            var name = Name ?? parameterInfo.Name;
            return new QueryBinding(parameterInfo.ParameterType, name, Default);
        }

        public string Name { get; set; }

        //TODO: Change from string
        public string Default { get; set; }
    }
}