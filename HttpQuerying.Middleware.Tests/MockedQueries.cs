using System;
using System.Threading;
using System.Threading.Tasks;
using HttpQuerying.Infrastructure;

namespace HttpQuerying.Middleware.Tests
{
    internal class StringQueryResult 
    {
        internal string StringProp { get; set; }
    }
    
    internal class QueryResult
    {
        internal string StringProp { get; set; }
        internal int IntProp { get; set; }

        internal QueryResult ObjectProp { get; set; }
    }

    internal class TestQuery : IQuery
    {
    }

    internal class TestQueryHandler : IQueryHandler<TestQuery, QueryResult>
    {
        public Task<QueryResult> HandleAsync(TestQuery query, Guid queryId, CancellationToken token) =>
            Task.FromResult( new QueryResult
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
        internal bool Flag { get; set; }
    }

    internal class TestConditionQuery : IQuery
    {
        internal bool Flag { get; set; }
    }

    internal class TestConditionQueryHandler : IQueryHandler<TestConditionQuery, ConditionQueryResult>
    {
        public Task<ConditionQueryResult> HandleAsync(TestConditionQuery query, Guid queryId, CancellationToken token) =>
            Task.FromResult(new ConditionQueryResult
            {
                Flag = query.Flag
            });
    }
}