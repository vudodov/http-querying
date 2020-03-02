using HttpQuerying.Infrastructure;

namespace HttpQuerying.Middleware
{
    internal class QueryMemoryCacheKey
    {
        public string Name { get; set; }
        public IQuery Query { get; set; }
    }
}