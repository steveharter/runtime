// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static partial class JsonNodeTests
    {
        [Fact]
        public static void TestAsJsonElement()
        {
            var jsonObject = new JsonObject
            {
                { "text", "property value" },
                { "boolean", true },
                { "number", 15 },
                { "null node", null },
                { "array", new JsonArray { "value1", "value2"} }
            };

            JsonElement jsonElement = jsonObject.AsJsonElement();
            Assert.False(jsonElement.IsImmutable);

            JsonElement.ObjectEnumerator enumerator = jsonElement.EnumerateObject();

            enumerator.MoveNext();
            Assert.Equal("text", enumerator.Current.Name);

            // JsonNodes do not get assigned a ValueKind.
            Assert.Equal(JsonValueKind.Undefined, enumerator.Current.Value.ValueKind);
            // Assert.Equal(JsonValueKind.String, enumerator.Current.Value.ValueKind);

            Assert.Equal("property value", enumerator.Current.Value.GetString());

            enumerator.MoveNext();
            Assert.Equal("boolean", enumerator.Current.Name);
            // JsonNodes do not get assigned a ValueKind.
            Assert.Equal(JsonValueKind.Undefined, enumerator.Current.Value.ValueKind);
            // Assert.Equal(JsonValueKind.True, enumerator.Current.Value.ValueKind);
            Assert.True(enumerator.Current.Value.GetBoolean());

            enumerator.MoveNext();
            Assert.Equal("number", enumerator.Current.Name);
            Assert.Equal(15, enumerator.Current.Value.GetInt32());
            // JsonNodes do not get assigned a ValueKind.
            Assert.Equal(JsonValueKind.Undefined, enumerator.Current.Value.ValueKind);
            //Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

            enumerator.MoveNext();
            Assert.Equal("null node", enumerator.Current.Name);
            Assert.Equal(JsonValueKind.Null, enumerator.Current.Value.ValueKind);

            enumerator.MoveNext();
            Assert.Equal("array", enumerator.Current.Name);
            Assert.Equal(2, enumerator.Current.Value.GetArrayLength());
            Assert.Equal(JsonValueKind.Array, enumerator.Current.Value.ValueKind);
            JsonElement.ArrayEnumerator innerEnumerator = enumerator.Current.Value.EnumerateArray();

            innerEnumerator.MoveNext();
            // JsonNodes do not get assigned a ValueKind.
            Assert.Equal(JsonValueKind.Undefined, innerEnumerator.Current.ValueKind);
            //Assert.Equal(JsonValueKind.String, innerEnumerator.Current.ValueKind);
            Assert.Equal("value1", innerEnumerator.Current.GetString());

            innerEnumerator.MoveNext();
            // JsonNodes do not get assigned a ValueKind.
            Assert.Equal(JsonValueKind.Undefined, innerEnumerator.Current.ValueKind);
            //Assert.Equal(JsonValueKind.String, innerEnumerator.Current.ValueKind);
            Assert.Equal("value2", innerEnumerator.Current.GetString());

            Assert.False(innerEnumerator.MoveNext());
            innerEnumerator.Dispose();

            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();

            // Modifying JsonObject will change JsonElement:
            jsonObject["text"] = 123;
            Assert.Equal(123, jsonElement.GetProperty("text").GetInt32());
        }

        [Fact]
        public static void TestArrayIterator()
        {
            JsonArray array = new JsonArray { 1, 2, 3 };
            JsonElement jsonNodeElement = array.AsJsonElement();
            IEnumerator enumerator = jsonNodeElement.EnumerateArray();
            array.Add(4);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public static void TestGetNode()
        {
            var jsonObject = new JsonObject
            {
                { "text", "property value" },
                { "boolean", true },
                { "number", 15 },
                { "null node", (JsonNode) null },
                { "array", new JsonArray { "value1", "value2"} }
            };

            JsonElement jsonElement = jsonObject.AsJsonElement();
            JsonObject jsonObjectFromElementGetNode = (JsonObject)JsonNode.GetNode(jsonElement);

            Assert.True(JsonNode.TryGetNode(jsonElement, out JsonNode jsonNodeFromElementTryGetNode));
            var jsonObjectFromElementTryGetNode = (JsonObject)jsonNodeFromElementTryGetNode;

            Assert.Equal("property value", (string)jsonObjectFromElementGetNode["text"]);
            Assert.Equal("property value", (string)jsonObjectFromElementTryGetNode["text"]);

            // Modifying JsonObject will change jsonObjectFromElementGetNode and jsonObjectFromElementTryGetNode:

            jsonObject["text"] = "something different";
            Assert.Equal("something different", (string)jsonObjectFromElementGetNode["text"]);
            Assert.Equal("something different", (string)jsonObjectFromElementTryGetNode["text"]);

            // Modifying JsonObjectFromElementGetNode will change JsonObject and jsonObjectFromElementTryGetNode:

            jsonObjectFromElementGetNode["boolean"] = false;
            Assert.False(((JsonValue<bool>)jsonObject["boolean"]).Value);
            Assert.False(((JsonValue<bool>)jsonObjectFromElementTryGetNode["boolean"]).Value);
        }

        [Fact]
        public static void TestGetNodeFails()
        {
            JsonDocument jsonDocument = JsonDocument.Parse("{}");
            JsonElement jsonElement = jsonDocument.RootElement;
            Assert.False(JsonNode.TryGetNode(jsonElement, out JsonNode _));
            Assert.Throws<ArgumentException>(() => JsonNode.GetNode(jsonElement));

            jsonElement = default;
            Assert.False(JsonNode.TryGetNode(jsonElement, out JsonNode _));
            Assert.Throws<ArgumentException>(() => JsonNode.GetNode(jsonElement));
        }
    }
}
