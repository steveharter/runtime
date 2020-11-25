// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class JsonNodeDynamicTests
    {
        private const string ExpectedDomJson = "{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[2,3,42]," +
            "\"MyInt\":43,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
            "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

        private enum MyCustomEnum
        {
            Default = 0,
            FortyTwo = 42,
            Hello = 77
        }

        [Fact]
        public static void VerifyPrimitives()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // JsonDynamicString has an implicit cast to string.
            Assert.IsAssignableFrom<JsonValue>(obj.MyString);
            Assert.Equal("Hello", (string)obj.MyString);

            // Verify other string-based types.
            Assert.Equal(MyCustomEnum.Hello, (MyCustomEnum)obj.MyString);
            Assert.Equal(DynamicTests.MyDateTime, (DateTime)obj.MyDateTime);
            Assert.Equal(DynamicTests.MyGuid, (Guid)obj.MyGuid);

            // JsonDynamicBoolean has an implicit cast to bool.
            Assert.IsAssignableFrom<JsonValue>(obj.MyBoolean);
            bool b = obj.MyBoolean;
            Assert.True(b);

            // Numbers must specify the type through a cast or assignment.
            Assert.IsAssignableFrom<JsonValue>(obj.MyInt);
            Assert.ThrowsAny<Exception>(() => obj.MyInt == 42L);
            Assert.Equal(42L, (long)obj.MyInt);
            Assert.Equal((byte)42, (byte)obj.MyInt);

            // Verify int-based Enum support through "unknown number type" fallback.
            Assert.Equal(MyCustomEnum.FortyTwo, (MyCustomEnum)obj.MyInt);

            // Verify floating point.
            obj = JsonSerializer.Deserialize<object>("4.2", options);
            Assert.IsAssignableFrom<JsonValue>(obj);

            double dbl = (double)obj;
#if !BUILDING_INBOX_LIBRARY
            string temp = dbl.ToString();
            // The reader uses "G17" format which causes temp to be 4.2000000000000002 in this case.
            dbl = double.Parse(temp, System.Globalization.CultureInfo.InvariantCulture);
#endif
            Assert.Equal(4.2, dbl);
        }

        [Fact]
        public static void VerifyArray()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            Assert.IsAssignableFrom<JsonObject>(obj);
            Assert.IsAssignableFrom<JsonArray>(obj.MyArray);

            Assert.Equal(2, obj.MyArray.Count);
            Assert.Equal(1, (int)obj.MyArray[0]);
            Assert.Equal(2, (int)obj.MyArray[1]);

            // Ensure we can enumerate
            int count = 0;
            foreach (object value in obj.MyArray)
            {
                count++;
            }
            Assert.Equal(2, count);

            // Ensure we can mutate through indexers
            obj.MyArray[0] = 10;
            Assert.Equal(10, (int)obj.MyArray[0]);
        }

        [Fact]
        public static void JsonDynamicTypes_Serialize()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            // Guid (string)
            string GuidJson = $"{DynamicTests.MyGuid.ToString("D")}";
            string GuidJsonWithQuotes = $"\"{GuidJson}\"";

            dynamic dynamicString = new DynamicJsonValue(GuidJson, options);
            Assert.Equal(DynamicTests.MyGuid, (Guid)dynamicString);
            string json = dynamicString.Serialize();
            Assert.Equal(GuidJsonWithQuotes, json);

            // char (string)
            dynamicString = new DynamicJsonValue("a", options);
            Assert.Equal('a', (char)dynamicString);
            json = dynamicString.Serialize();
            Assert.Equal("\"a\"", json);

            // Number (JsonElement)
            using (JsonDocument doc = JsonDocument.Parse($"{decimal.MaxValue}"))
            {
                dynamic dynamicNumber = new DynamicJsonValue(doc.RootElement, options);
                Assert.Equal<decimal>(decimal.MaxValue, (decimal)dynamicNumber);
                json = dynamicNumber.Serialize();
                Assert.Equal(decimal.MaxValue.ToString(), json);
            }

            // Boolean
            dynamic dynamicBool = new DynamicJsonValue(true, options);
            Assert.True((bool)dynamicBool);
            json = dynamicBool.Serialize();
            Assert.Equal("true", json);

            // Array
            dynamic arr = new DynamicJsonArray(options);
            arr.Add(1);
            arr.Add(2);
            json = arr.Serialize();
            Assert.Equal("[1,2]", json);

            // Object
            dynamic dynamicObject = new DynamicJsonObject(options);
            dynamicObject.One = 1;
            dynamicObject.Two = 2;

            json = dynamicObject.Serialize();
            JsonTestHelper.AssertJsonEqual("{\"One\":1,\"Two\":2}", json);
        }

        [Fact]
        public static void JsonDynamicTypes_Deserialize()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            Assert.IsType<DynamicJsonObject>(JsonSerializer.Deserialize<DynamicJsonObject>("{}", options));
            Assert.IsType<DynamicJsonObject>(JsonSerializer.Deserialize<object>("{}", options));
            Assert.IsType<JsonObject>(JsonSerializer.Deserialize<JsonNode>("{}", options));
            Assert.IsType<DynamicJsonArray>(JsonSerializer.Deserialize<DynamicJsonArray>("[]", options));
            Assert.IsType<DynamicJsonArray>(JsonSerializer.Deserialize<object>("[]", options));
            Assert.IsType<JsonArray>(JsonSerializer.Deserialize<JsonNode>("[]", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<DynamicJsonValue>("true", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<object>("true", options));
            Assert.IsType<JsonValue>(JsonSerializer.Deserialize<JsonNode>("true", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<DynamicJsonValue>("0", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<object>("0", options));
            Assert.IsType<JsonValue>(JsonSerializer.Deserialize<JsonNode>("0", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<DynamicJsonValue>("1.2", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<object>("1.2", options));
            Assert.IsType<JsonValue>(JsonSerializer.Deserialize<JsonNode>("1.2", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<DynamicJsonValue>("\"str\"", options));
            Assert.IsType<DynamicJsonValue>(JsonSerializer.Deserialize<object>("\"str\"", options));
            Assert.IsType<JsonValue>(JsonSerializer.Deserialize<JsonNode>("\"str\"", options));
        }

        /// <summary>
        /// Use a mutable DOM with the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_UsingDynamicKeyword()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // Change some primitives.
            obj.MyString = new JsonValue("Hello!", options);
            obj.MyBoolean = new JsonValue(false, options);
            obj.MyInt = new JsonValue(43, options);

            // Add nested objects.
            // Use JsonDynamicObject; ExpandoObject should not be used since it doesn't have the same semantics including
            // null handling and case-sensitivity that respects JsonSerializerOptions.PropertyNameCaseInsensitive.
            dynamic myObject = new DynamicJsonObject(options);
            myObject.MyString = "Hello!!";
            obj.MyObject = myObject;

            dynamic child = new DynamicJsonObject(options);
            child.ChildProp = 1;
            obj.Child = child;

            // Modify number elements.
            dynamic arr = obj.MyArray;
            arr[0] = (int)arr[0] + 1;
            arr[1] = (int)arr[1] + 1;

            // Add an element.
            arr.Add(new JsonValue(42));

            string json = obj.Serialize();
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        [Fact]
        public static void DynamicObject_CaseSensitivity()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            dynamic obj = JsonSerializer.Deserialize<object>("{\"MyProperty\":42}", options);

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Null(obj.myProperty);
            Assert.Null(obj.MYPROPERTY);

            options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<object>("{\"MyProperty\":42}", options);

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Equal(42, (int)obj.myproperty);
            Assert.Equal(42, (int)obj.MYPROPERTY);
        }

        [Fact]
        public static void DynamicObject_MissingProperty()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            dynamic obj = JsonSerializer.Deserialize<object>("{}", options);
            Assert.Equal(null, obj.NonExistingProperty);
        }
    }
}
#endif
