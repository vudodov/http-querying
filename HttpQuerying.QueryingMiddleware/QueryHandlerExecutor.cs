using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HttpQuerying.QueryingMiddleware
{
    internal static class QueryHandlerExecutor
    {
        internal static async Task<object> Execute(Type queryType, Type queryHandlerType, Guid queryId,
            PipeReader pipeReader,
            IServiceProvider serviceProvider, CancellationToken cancellationToken,
            JsonSerializerOptions jsonSerializerOptions)
        {
            object queryHandlerInstance = ActivatorUtilities.CreateInstance(serviceProvider, queryHandlerType);
            MethodInfo? handleAsyncMethod = queryHandlerType.GetMethod("HandleAsync");
            
            if (handleAsyncMethod == null) throw new MissingMethodException(nameof(queryHandlerType), "HandleAsync");

            var query = await ReadCommandAsync(pipeReader, queryType, jsonSerializerOptions, cancellationToken);

            var queryResult = handleAsyncMethod.Invoke(queryHandlerInstance,
                new[] {query, queryId, cancellationToken});
            
            if (queryResult == null) throw new NullReferenceException("Command result cannot be null.");

            return await (Task<object>) queryResult;
        }

        private static async Task<object?> ReadCommandAsync(PipeReader pipeReader, Type queryType,
            JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readResult = await pipeReader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;
                pipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (readResult.IsCompleted)
                {
                    return buffer.IsEmpty
                        ? null
                        : buffer.IsSingleSegment
                            ? JsonSerializer.Deserialize(buffer.FirstSpan, queryType, jsonSerializerOptions)
                            : DeserializeSequence(buffer, queryType);
                }
            }

            throw new TaskCanceledException();
        }

        private static object DeserializeSequence(ReadOnlySequence<byte> buffer, Type queryType)
        {
            var jsonReader = new Utf8JsonReader(buffer);
            return JsonSerializer.Deserialize(ref jsonReader, queryType);
        }
    }
}