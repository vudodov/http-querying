using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HttpQuerying.Infrastructure;

namespace HttpQuerying.Playground.Queries
{
    public class Potato
    {
        public string Name { get; set; }
    }
    
    public class GetAllPotatoesQuery : IQuery
    {
        
    }
    
    public class GetAllPotatoesQueryHandler : IQueryHandler<GetAllPotatoesQuery, List<Potato>>
    {
        public Task<List<Potato>> HandleAsync(GetAllPotatoesQuery query, Guid queryId, CancellationToken token)
        {
            return Task.FromResult(new List<Potato> {new Potato {Name = "Nick"}, new Potato {Name = "Joseph"}});
        }
    }
}