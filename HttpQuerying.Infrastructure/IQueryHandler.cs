using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpQuerying.Infrastructure
{
    public interface IQueryHandler<in TQuery, TQueryResult> where TQuery : IQuery
    {
        Task<TQueryResult> HandleAsync(TQuery query, Guid queryId, CancellationToken token);
    }
}