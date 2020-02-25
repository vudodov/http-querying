using System;

namespace HttpQuerying.QueryingMiddleware
{
    internal sealed class HttpQueryResult
    {
        public Guid QueryId { get; set; }
        public object Result { get; set; }
    }
}