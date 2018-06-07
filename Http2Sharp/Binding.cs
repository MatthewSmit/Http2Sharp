using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal abstract class Binding
    {
        [NotNull] private readonly Type type;

        protected Binding([NotNull] Type type)
        {
            this.type = type;
        }

        public abstract object Bind([NotNull] Dictionary<string, string> parameters, [NotNull] IEnumerable<(string, string)> queries, [CanBeNull] object body);

        protected object ConvertType(string value)
        {
            return Convert.ChangeType(value, type);
        }

        protected object ConvertType(object value)
        {
            throw new NotImplementedException();
        }
    }
}