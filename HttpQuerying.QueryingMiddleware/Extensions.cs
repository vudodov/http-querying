using System;
using System.Reflection;
using DependencyRegistry;
using HttpQuerying.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HttpQuerying.QueryingMiddleware
{
    public static class Extensions
    {
        public static IApplicationBuilder UseHttpQuerying(this IApplicationBuilder builder)
            => builder.UseMiddleware<Middleware>();

        /// <summary>
        /// Use Querying Middleware with built in ETag support.
        /// </summary>
        /// <param name="cacheOptions">Used to configure in-memory cache options</param>
        /// <param name="getCacheKey">Used to define how to calculate the cache key</param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpQuerying(this IApplicationBuilder builder,
            MemoryCacheEntryOptions cacheOptions,
            Func<string, IQuery, object> getCacheKey) => builder.UseMiddleware<CacheMiddleware>(cacheOptions, getCacheKey);

        public static void AddHttpQuerying(this IServiceCollection serviceCollection, params Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IRegistry<IQuery>>(assemblies.Length == 0
                ? new Registry<IQuery>()
                : new Registry<IQuery>(assemblies));
            serviceCollection.AddMemoryCache();
        }
    }
}