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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation
{
    public class QueryableToAggregateFluentTests
    {
        [Fact]
        public void ToAggregateFluent_should_produce_the_expected_stages()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(product => product.Price > 10)
                .OrderBy(product => product.Name)
                .Select(product => new ProductView { Name = product.Name, Price = product.Price });

            var stages = Render(collection, queryable.ToAggregateFluent<Product, ProductView>());

            Linq3TestHelpers.AssertStages(
                stages,
                new[]
                {
                    "{ $match : { Price : { $gt : NumberDecimal('10') } } }",
                    "{ $sort : { Name : 1 } }",
                    "{ $project : { Name : '$Name', Price : '$Price', _id : 0 } }"
                });
        }

        [Fact]
        public void ToAggregateFluent_should_keep_a_filtered_inner_join_inside_the_lookup()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupJoin(
                    collection.AsQueryable().Where(related => related.Price > 10),
                    product => product.ParentId,
                    related => (int?)related.Id,
                    (product, parents) => new ProductView
                    {
                        Name = product.Name,
                        Price = product.Price
                    });

            var stages = Render(collection, queryable.ToAggregateFluent<Product, ProductView>());

            var lookup = stages.Single(stage => stage.Contains("$lookup"))["$lookup"].AsBsonDocument;
            var innerPipeline = lookup["pipeline"].AsBsonArray.Select(stage => stage.AsBsonDocument).ToList();

            innerPipeline.Should().ContainSingle(stage =>
                stage.Contains("$match") && stage["$match"].ToString().Contains("Price"));
        }

        [Fact]
        public void ToAggregateFluent_should_apply_the_pipeline_optimizer()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(product => product.Category)
                .Select(group => new GroupView { Category = group.Key, Total = group.Sum(product => product.Price) });

            var stages = Render(collection, queryable.ToAggregateFluent<Product, GroupView>());

            // Unoptimized, a grouping pushes every document into the group and sums client-side
            // shapes afterwards. Only the pipeline optimizer rewrites it into a server-side $sum,
            // so these stages fail if the optimization step is ever skipped.
            Linq3TestHelpers.AssertStages(
                stages,
                new[]
                {
                    "{ $group : { _id : '$Category', __agg0 : { $sum : '$Price' } } }",
                    "{ $project : { Category : '$_id', Total : '$__agg0', _id : 0 } }"
                });
        }

        [Fact]
        public void ToAggregateFluent_should_carry_the_output_serializer_of_the_translated_pipeline()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(product => product.Price);

            var aggregate = queryable.ToAggregateFluent<Product, decimal>();

            var renderedPipeline = ((AggregateFluent<Product, decimal>)aggregate).Pipeline.Render(
                new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry));

            // A scalar projection is wrapped by the translator, so the registry's default decimal
            // serializer would read the result back from the wrong element.
            renderedPipeline.OutputSerializer.Should().NotBeSameAs(
                BsonSerializer.SerializerRegistry.GetSerializer<decimal>());
        }

        [Fact]
        public void ToAggregateFluent_should_carry_the_aggregate_options_of_the_provider()
        {
            var collection = CreateCollection();
            var options = new AggregateOptions { AllowDiskUse = true, MaxTime = TimeSpan.FromSeconds(42) };

            var aggregate = collection.AsQueryable(options).ToAggregateFluent<Product, Product>();

            aggregate.Options.AllowDiskUse.Should().BeTrue();
            aggregate.Options.MaxTime.Should().Be(TimeSpan.FromSeconds(42));
        }

        [Fact]
        public void ToAggregateFluent_should_produce_an_empty_pipeline_for_an_unmodified_queryable()
        {
            var collection = CreateCollection();

            var stages = Render(collection, collection.AsQueryable().ToAggregateFluent<Product, Product>());

            stages.Should().BeEmpty();
        }

        [Fact]
        public void ToAggregateFluent_should_throw_when_the_source_is_not_a_mongodb_queryable()
        {
            var queryable = new[] { new Product() }.AsQueryable();

            var exception = Record.Exception(() => queryable.ToAggregateFluent<Product, Product>());

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("source");
            argumentException.Message.Should().Contain("MongoDB IQueryable");
        }

        [Fact]
        public void ToAggregateFluent_should_throw_when_the_source_is_built_against_a_database()
        {
            var database = CreateDatabase();

            var queryable = database.AsQueryable().Documents(new Product());

            var exception = Record.Exception(() => queryable.ToAggregateFluent<NoPipelineInput, Product>());

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("source");
            argumentException.Message.Should().Contain("not a database");
        }

        [Fact]
        public void ToAggregateFluent_should_throw_when_the_source_is_null()
        {
            IQueryable<Product> source = null;

            var exception = Record.Exception(() => source.ToAggregateFluent<Product, Product>());

            exception.Should().BeOfType<ArgumentNullException>();
        }

        private static IMongoCollection<Product> CreateCollection()
        {
            var database = CreateDatabase();
            var settings = new MongoCollectionSettings();
            var mockCollection = new Mock<IMongoCollection<Product>>();
            mockCollection.SetupGet(collection => collection.CollectionNamespace)
                .Returns(new CollectionNamespace(database.DatabaseNamespace, "products"));
            mockCollection.SetupGet(collection => collection.Database).Returns(database);
            mockCollection.SetupGet(collection => collection.DocumentSerializer)
                .Returns(BsonSerializer.SerializerRegistry.GetSerializer<Product>());
            mockCollection.SetupGet(collection => collection.Settings).Returns(settings);
            return mockCollection.Object;
        }

        private static IMongoDatabase CreateDatabase()
        {
            var client = new Mock<IMongoClient>();
            client.SetupGet(c => c.Settings).Returns(new MongoClientSettings());

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase.SetupGet(database => database.Client).Returns(client.Object);
            mockDatabase.SetupGet(database => database.DatabaseNamespace).Returns(new DatabaseNamespace("test"));
            mockDatabase.SetupGet(database => database.Settings).Returns(new MongoDatabaseSettings());
            return mockDatabase.Object;
        }

        private static System.Collections.Generic.List<BsonDocument> Render<TResult>(
            IMongoCollection<Product> collection,
            IAggregateFluent<TResult> aggregate)
        {
            return Linq3TestHelpers.Translate(collection, aggregate);
        }

        public class Product
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public string Category { get; set; }
        }

        public class ProductView
        {
            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        public class GroupView
        {
            public string Category { get; set; }
            public decimal Total { get; set; }
        }
    }
}
