/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Extension methods for converting a LINQ query into an <see cref="IAggregateFluent{TResult}"/>.
    /// </summary>
    public static class QueryableToAggregateFluentExtensions
    {
        /// <summary>
        /// Translates a LINQ query into the equivalent aggregation pipeline and returns it as an
        /// <see cref="IAggregateFluent{TResult}"/> over the collection the query was built from.
        /// </summary>
        /// <remarks>
        /// This lets a query be expressed in LINQ — where features such as query filters and joins
        /// are applied automatically — and then handed to APIs that accept only an
        /// <see cref="IAggregateFluent{TResult}"/>. The query is translated through the driver's own
        /// translation path, the same one executing it would take, so both forms send identical
        /// stages to the server. The query is not executed by this method.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the source collection.</typeparam>
        /// <typeparam name="TResult">The type of the documents produced by the query.</typeparam>
        /// <param name="source">The LINQ query. It must be a MongoDB queryable built against a collection.</param>
        /// <returns>An aggregate fluent whose pipeline is the translation of the query.</returns>
        /// <exception cref="ArgumentException">
        /// The source is not a MongoDB queryable over <typeparamref name="TDocument"/>, or it was
        /// built against a database rather than a collection.
        /// </exception>
        public static IAggregateFluent<TResult> ToAggregateFluent<TDocument, TResult>(this IQueryable<TResult> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            if (source.Provider is not MongoQueryProvider<TDocument> provider)
            {
                var actual = source.Provider is MongoQueryProvider
                    ? "a MongoDB IQueryable over a different document type"
                    : "not a MongoDB IQueryable";

                throw new ArgumentException(
                    $"The source argument must be a MongoDB IQueryable over {typeof(TDocument)}, but it is {actual}. " +
                    $"The first type argument must be the document type of the collection the query was built from.",
                    nameof(source));
            }

            if (provider.Collection == null)
            {
                throw new ArgumentException(
                    "The source argument must be a MongoDB IQueryable against a collection, not a database.",
                    nameof(source));
            }

            // Reuse the driver's own translation entry point rather than re-deriving the
            // preprocess/translate/optimize sequence, so these stages cannot drift from the ones
            // executing the query would send.
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TResult>(
                provider,
                source.Expression,
                provider.GetTranslationOptions());

            var translatedPipeline = executableQuery.Pipeline;
            var stages = translatedPipeline.Ast.Render().AsBsonArray.Cast<BsonDocument>().ToArray();

            PipelineDefinition<TDocument, TResult> pipeline = new BsonDocumentStagePipelineDefinition<TDocument, TResult>(
                stages,
                AdaptOutputSerializer<TResult>(translatedPipeline.OutputSerializer));

            return new CollectionAggregateFluent<TDocument, TResult>(
                provider.Session,
                provider.Collection,
                pipeline,
                provider.Options ?? new AggregateOptions());
        }

        // Mirrors ExecutableQuery.GetOutputSerializer. The translator's serializer describes the
        // shape the rendered stages actually produce, so letting it fall back to the registry
        // default would read results back from the wrong elements.
        private static IBsonSerializer<TResult> AdaptOutputSerializer<TResult>(IBsonSerializer outputSerializer)
        {
            var outputType = outputSerializer.ValueType;

            if (outputType == typeof(TResult))
            {
                return (IBsonSerializer<TResult>)outputSerializer;
            }

            if (!typeof(TResult).IsAssignableFrom(outputType))
            {
                throw new NotSupportedException(
                    $"The type of the pipeline output is {outputType} which is not assignable to {typeof(TResult)}.");
            }

            if (typeof(TResult).IsNullableOf(outputType))
            {
                return (IBsonSerializer<TResult>)NullableSerializer.Create(outputSerializer);
            }

            return (IBsonSerializer<TResult>)DowncastingSerializer.Create(typeof(TResult), outputType, outputSerializer);
        }
    }
}
