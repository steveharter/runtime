// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class JsonNodeTests
    {
        private const string ExpectedDomJson = "{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[2,3,42]," +
            "\"MyInt\":43,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
            "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

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

            Assert.Equal(42, obj["MyProperty"].To<int>());
            Assert.Null(obj["myproperty"]);
            Assert.Null(obj["MYPROPERTY"]);

            options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].To<int>());
            Assert.Equal(42, obj["myproperty"].To<int>());
            Assert.Equal(42, obj["MYPROPERTY"].To<int>());
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
            Assert.Equal(42, obj.To<int>());

            obj = JsonSerializer.Deserialize<JsonNode>("\"NaN\"", options);
            Assert.IsAssignableFrom<JsonValue>(obj);
            Assert.Equal(double.NaN, obj.To<double>());
            Assert.Equal(float.NaN, obj.To<float>());
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
    }
}
