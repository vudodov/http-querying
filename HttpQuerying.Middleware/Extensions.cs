using System;
using System.Reflection;
using DependencyRegistry;
using HttpQuerying.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HttpQuerying.Middleware
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
        /// <param name="etagCacheComputation">Used to calculate the ETag value</param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpQuerying(this IApplicationBuilder builder,
            MemoryCacheEntryOptions cacheOptions, 
            Func<string, IQuery, object> getCacheKey,
            Func<object, string> etagCacheComputation) =>
            builder.UseMiddleware<CacheMiddleware>(cacheOptions, getCacheKey, etagCacheComputation);

        public static void AddHttpQuerying(this IServiceCollection serviceCollection, params Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IRegistry<IQuery>>(assemblies.Length == 0
                ? new Registry<IQuery>(new[] {Assembly.GetCallingAssembly()})
                : new Registry<IQuery>(assemblies));

            serviceCollection.AddMemoryCache();
        }
    }
}