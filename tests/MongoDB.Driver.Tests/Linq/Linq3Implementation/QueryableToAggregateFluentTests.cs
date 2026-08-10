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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation
{
    public class QueryableToAggregateFluentTests : Linq3IntegrationTest
    {
        [Fact]
        public void ToAggregateFluent_should_produce_the_same_stages_as_the_queryable()
        {
            var collection = GetCollection<Product>();

            var queryable = collection.AsQueryable()
                .Where(product => product.Price > 10)
                .OrderBy(product => product.Name)
                .Select(product => new ProductView { Name = product.Name, Price = product.Price });

            var expectedStages = Linq3TestHelpers.Translate<Product, ProductView>(collection, queryable);

            var aggregate = queryable.ToAggregateFluent<Product, ProductView>();
            var actualStages = Linq3TestHelpers.Translate(collection, aggregate);

            actualStages.Should().Equal(expectedStages);
        }

        [Fact]
        public void ToAggregateFluent_should_keep_a_filtered_inner_join_correlated()
        {
            var collection = GetCollection<Product>();

            var queryable = collection.AsQueryable()
                .Where(product => product.Price > 10)
                .GroupJoin(
                    collection.AsQueryable().Where(related => related.Price > 10),
                    product => product.ParentId,
                    related => (int?)related.Id,
                    (product, parents) => new ProductView
                    {
                        Name = product.Name,
                        Price = product.Price
                    });

            var expectedStages = Linq3TestHelpers.Translate<Product, ProductView>(collection, queryable);

            var aggregate = queryable.ToAggregateFluent<Product, ProductView>();
            var actualStages = Linq3TestHelpers.Translate(collection, aggregate);

            actualStages.Should().Equal(expectedStages);

            var lookup = actualStages.Single(stage => stage.Contains("$lookup"))["$lookup"].AsBsonDocument;
            lookup.Contains("pipeline").Should().BeTrue();
        }

        [Fact]
        public void ToAggregateFluent_should_throw_when_the_source_is_not_a_mongodb_queryable()
        {
            var queryable = new[] { new Product() }.AsQueryable();

            var exception = Record.Exception(() => queryable.ToAggregateFluent<Product, Product>());

            exception.Should().BeOfType<ArgumentException>();
        }

        private class Product
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        private class ProductView
        {
            public string Name { get; set; }
            public decimal Price { get; set; }
        }
    }
}
