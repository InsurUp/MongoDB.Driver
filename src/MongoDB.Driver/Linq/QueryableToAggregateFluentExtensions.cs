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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

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
        /// <see cref="IAggregateFluent{TResult}"/>. The pipeline is translated and optimized exactly
        /// as it would be when the query is executed, so both forms send the same stages to the server.
        /// The query is not executed by this method.
        /// </remarks>
        /// <typeparam name="TSource">The type of the documents in the source collection.</typeparam>
        /// <typeparam name="TResult">The type of the documents produced by the query.</typeparam>
        /// <param name="source">The LINQ query. It must be a MongoDB queryable built against a collection.</param>
        /// <returns>An aggregate fluent whose pipeline is the translation of the query.</returns>
        /// <exception cref="ArgumentException">
        /// The source is not a MongoDB queryable, or it was not built against a collection.
        /// </exception>
        public static IAggregateFluent<TResult> ToAggregateFluent<TSource, TResult>(this IQueryable<TResult> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            if (source.Provider is not MongoQueryProvider<TSource> provider)
            {
                throw new ArgumentException(
                    $"The source argument must be a MongoDB IQueryable against a collection of {typeof(TSource).Name}.",
                    nameof(source));
            }

            if (provider.Collection == null)
            {
                throw new ArgumentException(
                    "The source argument must be a MongoDB IQueryable against a collection.",
                    nameof(source));
            }

            var (stages, outputSerializer) = TranslateToStages<TSource, TResult>(provider, source);

            PipelineDefinition<TSource, TResult> pipeline = new BsonDocumentStagePipelineDefinition<TSource, TResult>(
                stages,
                outputSerializer as IBsonSerializer<TResult>);

            return new CollectionAggregateFluent<TSource, TResult>(
                provider.Session,
                provider.Collection,
                pipeline,
                provider.Options ?? new AggregateOptions());
        }

        private static (BsonDocument[] Stages, IBsonSerializer OutputSerializer) TranslateToStages<TSource, TResult>(
            MongoQueryProvider<TSource> provider,
            IQueryable<TResult> source)
        {
            var translationOptions = provider.GetTranslationOptions();
            var expression = LinqExpressionPreprocessor.Preprocess(source.Expression);

            var context = TranslationContext.Create(translationOptions);
            var translatedPipeline = ExpressionToPipelineTranslator.Translate(context, expression);
            var optimizedAst = AstPipelineOptimizer.Optimize(translatedPipeline.Ast);

            var stages = optimizedAst.Render().AsBsonArray.Cast<BsonDocument>().ToArray();

            return (stages, translatedPipeline.OutputSerializer);
        }
    }
}
