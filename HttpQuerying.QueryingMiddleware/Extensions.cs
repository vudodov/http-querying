using System.Reflection;
using DependencyRegistry;
using HttpQuerying.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HttpQuerying.QueryingMiddleware
{
    public static class Extensions
    {
        public static IApplicationBuilder UseHttpQuerying(this IApplicationBuilder builder)
            => builder.UseMiddleware<Middleware>();

        public static void AddHttpQuerying(this IServiceCollection serviceCollection, params Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IRegistry>(assemblies.Length == 0
                ? new Registry<IQuery>()
                : new Registry<IQuery>(assemblies));
            serviceCollection.AddMemoryCache();
        }
    }
}