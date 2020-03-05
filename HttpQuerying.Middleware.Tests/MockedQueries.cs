using System;
using System.Threading;
using System.Threading.Tasks;
using HttpQuerying.Infrastructure;

namespace HttpQuerying.Middleware.Tests
{
    public class StringQueryResult 
    {
        public string StringProp { get; set; }
    }
    
    public class QueryResult
    {
        public string StringProp { get; set; }
        public int IntProp { get; set; }

        public QueryResult ObjectProp { get; set; }
    }

    public class TestQuery : IQuery
    {
    }

    public class TestQueryHandler : IQueryHandler<TestQuery, QueryResult>
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


    public class ConditionQueryResult
    {
        public bool Flag { get; set; }
    }

    public class TestConditionQuery : IQuery
    {
        public bool Flag { get; set; }
    }

    public class TestConditionQueryHandler : IQueryHandler<TestConditionQuery, ConditionQueryResult>
    {
        public Task<ConditionQueryResult> HandleAsync(TestConditionQuery query, Guid queryId, CancellationToken token) =>
            Task.FromResult(new ConditionQueryResult
            {
                Flag = query.Flag
            });
    }
}