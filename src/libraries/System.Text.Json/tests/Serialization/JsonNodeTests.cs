// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;
using System.Text.Json.Serialization.Tests.Schemas.OrderPayload;
using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static partial class JsonNodeTests
    {
        private const string ExpectedDomJson = "{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[2,3,42]," +
            "\"MyInt\":43,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
            "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

        [Fact]
        public static void CodeSample_Simple_Primitives()
        {
            // Implicit conversions from JsonNode<T> to T and from T to JsonNode<T>
            {
                var node = new JsonValue<int>(42);
                Assert.Equal(42, (int)node);

                int intValue = (int)node;
                Assert.Equal(42, intValue);
            }

            // To()
            {
                var node = new JsonValue<int>(42);
                Assert.Equal(42, node.GetValue<int>());

                // Conversions to base types work
                Assert.Equal<object>(42, node.GetValue<object>());

                // Conversions to other types throw even if explicit operators such as from 'int' to 'short'
                Assert.Throws<InvalidOperationException>(() => node.GetValue<short>());
            }
        }

        [Fact]
        public static void CodeSample_Simple_Serialization()
        {
            var jObj = new JsonObject
            {
                // Primitives
                ["MyString"] = new JsonValue<string>("Hello!"),
                ["MyNull"] = null,
                ["MyBoolean"] = new JsonValue<bool>(false),

                // Nested array
                ["MyArray"] = new JsonArray
                (
                    new JsonValue<int>(2),
                    new JsonValue<int>(3),
                    new JsonValue<int>(42)
                ),

                // Additional primitives
                ["MyInt"] = new JsonValue<int>(43),
                ["MyDateTime"] = new JsonValue<DateTime>(new DateTime(2020, 7, 8)),
                ["MyGuid"] = new JsonValue<Guid>(new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51")),

                // Nested objects
                ["MyObject"] = new JsonObject
                {
                    ["MyString"] = new JsonValue<string>("Hello!!")
                },

                ["Child"] = new JsonObject
                {
                    ["ChildProp"] = new JsonValue<int>(1)
                }
            };

            string json = jObj.Serialize();
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        [Fact]
        public static void CodeSample_Simple_Serialization_WithImplicitOperators()
        {
            var jObj = new JsonObject
            {
                // Primitives
                ["MyString"] = "Hello!",
                ["MyNull"] = null,
                ["MyBoolean"] = false,

                // Nested array
                ["MyArray"] = new JsonArray(2, 3, 42),

                // Additional primitives
                ["MyInt"] = 43,
                ["MyDateTime"] = new DateTime(2020, 7, 8),
                ["MyGuid"] = new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51"),

                // Nested objects
                ["MyObject"] = new JsonObject
                {
                    ["MyString"] = "Hello!!"
                },

                ["Child"] = new JsonObject()
                {
                    ["ChildProp"] = 1
                }
            };

            string json = jObj.Serialize();
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        [Fact]
        public static void JsonTypes_Deserialize()
        {
            var options = new JsonSerializerOptions();

            VerifyTypeAndKind<JsonObject>(JsonSerializer.Deserialize<JsonNode>("{}", options), JsonValueKind.Object);
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("{}", options));

            VerifyTypeAndKind<JsonArray>(JsonSerializer.Deserialize<JsonNode>("[]", options), JsonValueKind.Array);
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("[]", options));

            VerifyTypeAndKind<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("true", options), JsonValueKind.True);
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("true", options));

            VerifyTypeAndKind<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("0", options), JsonValueKind.Number);
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("0", options));

            VerifyTypeAndKind<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("1.2", options), JsonValueKind.Number);
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("1.2", options));

            VerifyTypeAndKind<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("\"str\"", options), JsonValueKind.String);
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("\"str\"", options));

            void VerifyTypeAndKind<T>(object obj, JsonValueKind kind)
            {
                Assert.IsType<T>(obj);
                Assert.Equal(kind, ((JsonNode)obj).ValueKind);
            }
        }

        /// <summary>
        /// Use a mutable DOM without the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_WithoutUsingDynamicKeyword()
        {
            JsonNode obj = JsonSerializer.Deserialize<JsonObject>(DynamicTests.Json);
            Verify();

            // Verify the values are round-trippable.
            ((JsonArray)obj["MyArray"]).RemoveAt(2);
            Verify();

            void Verify()
            {
                // Change some primitives.
                obj["MyString"] = new JsonValue<string>("Hello!");
                obj["MyBoolean"] = new JsonValue<bool>(false);
                obj["MyInt"] = new JsonValue<int>(43);

                // Add nested objects.
                obj["MyObject"] = new JsonObject();
                obj["MyObject"]["MyString"] = new JsonValue<string>("Hello!!");

                obj["Child"] = new JsonObject();
                obj["Child"]["ChildProp"] = new JsonValue<int>(1);

                // Modify number elements.
                obj["MyArray"][0] = new JsonValue<int>(2);
                obj["MyArray"][1] = new JsonValue<int>(3);

                // Add an element.
                ((JsonArray)obj["MyArray"]).Add(new JsonValue<int>(42));

                string json = obj.Serialize();
                JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
            }
        }

        [Fact]
        public static void MissingProperty()
        {
            var options = new JsonSerializerOptions();
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{}", options);
            Assert.Null(obj["NonExistingProperty"]);
        }

        [Fact]
        public static void CaseSensitivity()
        {
            var options = new JsonSerializerOptions();
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].GetValue<int>());
            Assert.Null(obj["myproperty"]);
            Assert.Null(obj["MYPROPERTY"]);

            options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].GetValue<int>());
            Assert.Equal(42, obj["myproperty"].GetValue<int>());
            Assert.Equal(42, obj["MYPROPERTY"].GetValue<int>());
        }

        [Fact]
        public static void NamingPoliciesAreNotUsed()
        {
            const string Json = "{\"myProperty\":42}";

            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = new SimpleSnakeCasePolicy();

            JsonObject obj = JsonSerializer.Deserialize<JsonObject>(Json, options);
            string json = obj.Serialize();
            JsonTestHelper.AssertJsonEqual(Json, json);
        }

        [Fact]
        public static void NullHandling()
        {
            var options = new JsonSerializerOptions();
            JsonNode obj = JsonSerializer.Deserialize<JsonNode>("null", options);
            Assert.Null(obj);
        }

        [Fact]
        public static void QuotedNumbers_Deserialize()
        {
            var options = new JsonSerializerOptions();
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals;

            JsonNode obj = JsonSerializer.Deserialize<JsonNode>("\"42\"", options);
            Assert.IsAssignableFrom<JsonValue>(obj);
            Assert.Equal(42, obj.GetValue<int>());

            obj = JsonSerializer.Deserialize<JsonNode>("\"NaN\"", options);
            Assert.IsAssignableFrom<JsonValue>(obj);
            Assert.Equal(double.NaN, obj.GetValue<double>());
            Assert.Equal(float.NaN, obj.GetValue<float>());
        }

        [Fact]
        public static void QuotedNumbers_Serialize()
        {
            var options = new JsonSerializerOptions();
            options.NumberHandling = JsonNumberHandling.WriteAsString;

            JsonValue obj = new JsonValue<long>(42, options);
            string json = obj.Serialize();
            Assert.Equal("\"42\"", json);

            obj = new JsonValue<double>(double.NaN, options);
            json = obj.Serialize();
            Assert.Equal("\"NaN\"", json);
        }

        //[Fact]
        //public static void DeserializePerf10000()
        //{
        //    List<Order> x = StreamTests.PopulateLargeObject(1);
        //    string json = JsonSerializer.Serialize(x);
        //    JsonArray jArray = JsonSerializer.Deserialize<JsonArray>(json);

        //    string json2 = jArray.Serialize();
        //    Assert.Equal(json, json2);

        //    var sw = new Stopwatch();
        //    sw.Start();

        //    for (long l = 0; l < 10000; l++)
        //    {
        //        jArray = JsonSerializer.Deserialize<JsonArray>(json);
        //    }

        //    sw.Stop();
        //    throw new Exception("PERF:" + sw.ElapsedMilliseconds);
        //}

        //[Fact]
        //public static void SerializePerf10000()
        //{
        //    List<Order> x = StreamTests.PopulateLargeObject(1);
        //    string json = JsonSerializer.Serialize(x);
        //    JsonArray jArray = JsonSerializer.Deserialize<JsonArray>(json);

        //    string json2 = jArray.Serialize();
        //    Assert.Equal(json, json2);

        //    var sw = new Stopwatch();
        //    sw.Start();

        //    for (long l = 0; l < 10000; l++)
        //    {
        //        jArray.Serialize();
        //    }

        //    sw.Stop();
        //    throw new Exception("PERF:" + sw.ElapsedMilliseconds);
        //}

        //[Fact]
        //public static void DeserializePerf10000_Newtonsoft()
        //{
        //    List<Order> x = StreamTests.PopulateLargeObject(1);
        //    string json = JsonSerializer.Serialize(x);
        //    Newtonsoft.Json.Linq.JToken jToken = Newtonsoft.Json.Linq.JToken.Parse(json);

        //    var sw = new Stopwatch();
        //    sw.Start();

        //    for (long l = 0; l < 10000; l++)
        //    {
        //        jToken = Newtonsoft.Json.Linq.JToken.Parse(json);
        //    }

        //    sw.Stop();
        //    throw new Exception("PERF_Newtonsoft:" + sw.ElapsedMilliseconds);
        //}

        //[Fact]
        //public static void SerializePerf10000_Newtonsoft()
        //{
        //    List<Order> x = StreamTests.PopulateLargeObject(1);
        //    string json = JsonSerializer.Serialize(x);
        //    Newtonsoft.Json.Linq.JToken jToken = Newtonsoft.Json.Linq.JToken.Parse(json);
        //    string json2 = jToken.ToString(Newtonsoft.Json.Formatting.None);
        //    //Assert.Equal(json, json2);

        //    var sw = new Stopwatch();
        //    sw.Start();

        //    for (long l = 0; l < 10000; l++)
        //    {
        //        jToken.ToString();
        //    }

        //    sw.Stop();
        //    throw new Exception("PERF_Newtonsoft:" + sw.ElapsedMilliseconds);
        //}
    }
}
