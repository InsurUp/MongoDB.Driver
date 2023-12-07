﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4813Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Where_BitArray_Count_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.BitArray.Count == 1);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { 'BitArray' : { $size : 1 } } }"); // LINQ2 translation is wrong

                var results = queryable.ToList();
                results.Should().HaveCount(0); // LINQ2 result is wrong
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { Count : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_Dictionary_Count_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Dictionary.Count == 1);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { 'Dictionary.Count' : 1 } }"); // LINQ2 translation is wrong

                var results = queryable.ToList();
                results.Should().HaveCount(0); // LINQ2 result is wrong
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_DictionaryAsArrayOfArrays_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.DictionaryAsArrayOfArrays.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { DictionaryAsArrayOfArrays : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_DictionaryAsArrayOfDocuments_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.DictionaryAsArrayOfDocuments.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { DictionaryAsArrayOfDocuments : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_DictionaryInterface_Count_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.DictionaryInterface.Count == 1);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { 'DictionaryInterface.Count' : 1 } }"); // LINQ2 translation is wrong

                var results = queryable.ToList();
                results.Should().HaveCount(0); // LINQ2 result is wrong
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_DictionaryInterfaceArrayOfArrays_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.DictionaryInterfaceAsArrayOfArrays.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { DictionaryInterfaceAsArrayOfArrays : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_DictionaryInterfaceArrayOfDocuments_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.DictionaryInterfaceAsArrayOfDocuments.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { DictionaryInterfaceAsArrayOfDocuments : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_List_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.List.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'List' : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_ListInterface_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.ListInterface.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'ListInterface' : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_BitArray_Count_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.BitArray.Count);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$BitArray' }, _id : 0 } }"); // LINQ2 translation is wrong

                var exception = Record.Exception(() => queryable.ToList());
                exception.Should().BeOfType<MongoCommandException>(); // LINQ2 query fails server side
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("is not represented as an array");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Count);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { Count : '$Count', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$Count', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_Dictionary_Count_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Dictionary.Count);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { Count : '$Dictionary.Count', _id : 0 } }"); // LINQ2 translation is wrong

                var results = queryable.ToList();
                results.Should().Equal(0, 0); // LINQ2 result is wrong
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("is not represented as an array");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_DictionaryAsArrayOfArrays_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.DictionaryAsArrayOfArrays.Count);

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();

            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$DictionaryAsArrayOfArrays' }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryAsArrayOfArrays' }, _id : 0 } }");
            }

            results.Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_DictionaryAsArrayOfDocuments_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.DictionaryAsArrayOfDocuments.Count);

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();

            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$DictionaryAsArrayOfDocuments' }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryAsArrayOfDocuments' }, _id : 0 } }");
            }

            results.Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_DictionaryInterface_Count_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.DictionaryInterface.Count);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { Count : '$DictionaryInterface.Count', _id : 0 } }"); // LINQ2 translation is wrong

                var results = queryable.ToList();
                results.Should().Equal(0, 0); // LINQ2 result is wrong
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("is not represented as an array");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_DictionaryInterfaceAsArrayOfArrays_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.DictionaryInterfaceAsArrayOfArrays.Count);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$DictionaryInterfaceAsArrayOfArrays' }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryInterfaceAsArrayOfArrays' }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_DictionaryInterfaceAsArrayOfDocuments_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.DictionaryInterfaceAsArrayOfDocuments.Count);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$DictionaryInterfaceAsArrayOfDocuments' }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryInterfaceAsArrayOfDocuments' }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_List_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.List.Count);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$List' }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $size : '$List' }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_ListInterface_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.ListInterface.Count);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $size : '$ListInterface' }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $size : '$ListInterface' }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C
                {
                    Id = 1,
                    BitArray = new BitArray(length: 1),
                    Count = 1,
                    Dictionary = new() { { "A", 1 } },
                    DictionaryAsArrayOfArrays = new() { { "A", 1 } },
                    DictionaryAsArrayOfDocuments = new() { { "A", 1 } },
                    DictionaryInterface = new Dictionary<string, int> { { "A", 1 } },
                    DictionaryInterfaceAsArrayOfArrays = new Dictionary<string, int> { { "A", 1 } },
                    DictionaryInterfaceAsArrayOfDocuments = new Dictionary<string, int> { { "A", 1 } },
                    List = new() { 1 },
                    ListInterface = new List<int>() { 1 }
                },
                new C
                {
                    Id = 2,
                    BitArray = new BitArray(length: 2),
                    Count = 2,
                    Dictionary = new() { { "A", 1 }, { "B", 2 } },
                    DictionaryAsArrayOfArrays = new() { { "A", 1 }, { "B", 2 } },
                    DictionaryAsArrayOfDocuments = new() { { "A", 1 }, { "B", 2 } },
                    DictionaryInterface = new Dictionary<string, int> { { "A", 1 }, { "B", 2 } },
                    DictionaryInterfaceAsArrayOfArrays = new Dictionary<string, int> { { "A", 1 }, { "B", 2 } },
                    DictionaryInterfaceAsArrayOfDocuments = new Dictionary<string, int> { { "A", 1 }, { "B", 2 } },
                    List = new() { 1, 2 },
                    ListInterface = new List<int> { 1, 2 }
                }); ;
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public BitArray BitArray { get; set; }
            public int Count { get; set; }
            public Dictionary<string, int> Dictionary { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)] public Dictionary<string, int> DictionaryAsArrayOfArrays { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)] public Dictionary<string, int> DictionaryAsArrayOfDocuments { get; set; }
            public IDictionary<string, int> DictionaryInterface { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)] public IDictionary<string, int> DictionaryInterfaceAsArrayOfArrays { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)] public IDictionary<string, int> DictionaryInterfaceAsArrayOfDocuments { get; set; }
            public List<int> List { get; set; }
            public IList<int> ListInterface { get; set; }
        }
    }
}
