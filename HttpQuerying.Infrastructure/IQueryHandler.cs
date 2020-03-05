using System;
using System.Threading;
using System.Threading.Tasks;
using DependencyRegistry;

namespace HttpQuerying.Infrastructure
{
    public interface IQueryHandler<in TQuery, TQueryResult> : IDepender<TQuery>
        where TQuery : IQuery
    {
        Task<TQueryResult> HandleAsync(TQuery query, Guid queryId, CancellationToken token);
    }
}
