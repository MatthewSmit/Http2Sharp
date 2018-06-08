using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class RouteMethod
    {
        [NotNull] private readonly object instance;
        [NotNull] private readonly MethodInfo method;
        [NotNull] private readonly Binding[] bindings;

        public RouteMethod([NotNull] object instance, [NotNull] MethodInfo method)
        {
            this.instance = instance;
            this.method = method;

            var parameters = method.GetParameters();
            bindings = new Binding[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var bindingAttribute = parameters[i].GetCustomAttributes<BindingAttribute>().Single();
                bindings[i] = bindingAttribute.CreateBinding(parameters[i]);
            }
        }

        public HttpResponse Invoke([NotNull] IReadOnlyDictionary<string, string> parameters, [NotNull] IReadOnlyList<(string, string)> queries, [CanBeNull] object body)
        {
            var arguments = new object[bindings.Length];
            for (var i = 0; i < bindings.Length; i++)
            {
                arguments[i] = bindings[i].Bind(parameters, queries, body);
            }

            return (HttpResponse)method.Invoke(instance, arguments);
        }
    }
}