using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpQuerying.Infrastructure
{
    public interface IQueryHandler<in TQuery> where TQuery : IQuery
    {
        Task<dynamic> HandleAsync(TQuery query, Guid queryId, CancellationToken token);
    }
}