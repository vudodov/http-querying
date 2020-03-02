using System;

namespace HttpQuerying.Middleware
{
    internal sealed class HttpQueryResult
    {
        public Guid QueryId { get; set; }
        public object Result { get; set; }
    }
}