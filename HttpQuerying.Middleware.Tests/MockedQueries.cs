using System;
using System.Threading;
using System.Threading.Tasks;
using HttpQuerying.Infrastructure;

namespace HttpQuerying.Middleware.Tests
{
    internal class QueryResult
    {
        public string StringProp { get; set; }
        public int IntProp { get; set; }

        public QueryResult ObjectProp { get; set; }
    }

    internal class TestQuery : IQuery
    {
    }

    internal class TestQueryHandler : IQueryHandler<TestQuery>
    {
        public Task<dynamic> HandleAsync(TestQuery query, Guid queryId, CancellationToken token) =>
            Task.FromResult((object) new QueryResult
            {
                StringProp = "TestString",
                IntProp = 12,
                ObjectProp = new QueryResult
                {
                    StringProp = "AnotherString",
                    IntProp = 1
                }
            });
    }


    internal class ConditionQueryResult
    {
        public bool Flag { get; set; }
    }

    internal class TestConditionQuery : IQuery
    {
        public bool Flag { get; set; }
    }

    internal class TestConditionQueryHandler : IQueryHandler<TestConditionQuery>
    {
        public Task<dynamic> HandleAsync(TestConditionQuery query, Guid queryId, CancellationToken token) =>
            Task.FromResult((object) new ConditionQueryResult
            {
                Flag = query.Flag
            });
    }
}