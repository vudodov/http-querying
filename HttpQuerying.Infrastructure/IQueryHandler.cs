using System.Threading;
using System.Threading.Tasks;

namespace HttpQuerying.Infrastructure
{
    public interface IQueryHandler<in TQuery> where TQuery : IQuery
    {
        Task<object> HandleAsync(TQuery query, CancellationToken token);
    }
}