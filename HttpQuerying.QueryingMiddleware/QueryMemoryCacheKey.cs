using HttpQuerying.Infrastructure;

namespace HttpQuerying.QueryingMiddleware
{
    internal class QueryMemoryCacheKey
    {
        public string Name { get; set; }
        public IQuery Query { get; set; }
    }
}