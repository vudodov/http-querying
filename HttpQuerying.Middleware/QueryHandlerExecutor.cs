using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HttpQuerying.Middleware
{
    internal static class QueryHandlerExecutor
    {
        public static async Task<(object? message, Func<Task<object>> handleQuery)> Execute(
            Type queryType, Type queryHandlerType, Guid queryId, PipeReader queryData, IServiceProvider serviceProvider, 
            JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
        {
            object messageHandlerInstance = ActivatorUtilities.CreateInstance(serviceProvider, queryHandlerType);
            MethodInfo? handleAsyncMethod = queryHandlerType.GetMethod("HandleAsync");

            if (handleAsyncMethod == null) throw new MissingMethodException(nameof(queryHandlerType), "HandleAsync");

            object? query = await ReadMessageAsync(queryData, queryType, jsonSerializerOptions, cancellationToken);

            async Task<object> HandleQuery()
            {
                Task task = (Task) handleAsyncMethod.Invoke(messageHandlerInstance, 
                    new[] {query, queryId, cancellationToken});
                await task.ConfigureAwait(false);
                return (object) ((dynamic) task).Result;
            }

            return (query, HandleQuery);
        }

        private static async Task<object?> ReadMessageAsync(PipeReader pipeReader, Type messageType,
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
                            ? JsonSerializer.Deserialize(buffer.FirstSpan, messageType, jsonSerializerOptions)
                            : DeserializeSequence(buffer, messageType);
                }
            }

            throw new TaskCanceledException();
        }

        private static object DeserializeSequence(ReadOnlySequence<byte> buffer, Type messageType)
        {
            var jsonReader = new Utf8JsonReader(buffer);
            return JsonSerializer.Deserialize(ref jsonReader, messageType);
        }
    }
}