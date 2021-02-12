// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;
using Xunit;

namespace System.Text.Json.Node.Tests
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
        public static void CodeSample_Simple_Serialization_WithImplicitOperators()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            dynamic jObj = new JsonObject(options);

            // Dynamic objects do not support object initializers, so they can't be used.

            // Primitives
            jObj.MyString = "Hello!";
            // Dynamic is selected over the implicit operator
            Assert.Equal(typeof(JsonValue<object>), jObj.MyString.GetType());

            jObj.MyNull = null;
            jObj.MyBoolean = false;

            // Nested array
            jObj.MyArray = new JsonArray(2, 3, 42);

            // Additional primitives
            jObj.MyInt = 43;
            jObj.MyDateTime = new DateTime(2020, 7, 8);
            jObj.MyGuid = new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51");

            // Nested objects
            jObj.MyObject = new JsonObject();
            jObj.MyObject.MyString = "Hello!!";

            jObj.Child = new JsonObject();
            jObj.Child.ChildProp = 1;

            string json = jObj.Serialize();
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        [Fact]
        public static void VerifyPrimitives()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // JsonValue created from a JSON string.
            Assert.IsAssignableFrom<JsonValue>(obj.MyString);
            Assert.Equal("Hello", (string)obj.MyString);

            // Verify other string-based types.
            Assert.Equal(MyCustomEnum.Hello, (MyCustomEnum)obj.MyString);
            Assert.Equal(DynamicTests.MyDateTime, (DateTime)obj.MyDateTime);
            Assert.Equal(DynamicTests.MyGuid, (Guid)obj.MyGuid);

            // JsonValue created from a JSON bool.
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
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
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
            // This is actually a JsonValue, not a JsonValue, due to the implicit operators.
            Assert.IsType<JsonValue<int>>(obj.MyArray[0]);

            Assert.Equal(10, (int)obj.MyArray[0]);
        }

        [Fact]
        public static void JsonTypes_Serialize()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            // Guid (string)
            string GuidJson = $"{DynamicTests.MyGuid.ToString("D")}";
            string GuidJsonWithQuotes = $"\"{GuidJson}\"";

            dynamic dynamicString = new JsonValue<string>(GuidJson, options);

            Assert.Equal(DynamicTests.MyGuid, dynamicString.Deserialize<Guid>());
            string json = dynamicString.Serialize();
            Assert.Equal(GuidJsonWithQuotes, json);

            // char (string)
            dynamicString = new JsonValue<string>("a", options);
            Assert.Equal('a', dynamicString.Deserialize<char>());
            json = dynamicString.Serialize();
            Assert.Equal("\"a\"", json);

            // Number (JsonElement)
            using (JsonDocument doc = JsonDocument.Parse($"{decimal.MaxValue}"))
            {
                dynamic dynamicNumber = new JsonValue<JsonElement>(doc.RootElement, options);
                Assert.Equal<decimal>(decimal.MaxValue, (decimal)dynamicNumber);
                json = dynamicNumber.Serialize();
                Assert.Equal(decimal.MaxValue.ToString(), json);
            }

            // Boolean
            dynamic dynamicBool = new JsonValue<bool>(true, options);
            Assert.True((bool)dynamicBool);
            json = dynamicBool.Serialize();
            Assert.Equal("true", json);

            // Array
            dynamic arr = new JsonArray(options);
            arr.Add(1);
            arr.Add(2);
            json = arr.Serialize();
            Assert.Equal("[1,2]", json);

            // Object
            dynamic dynamicObject = new JsonObject(options);
            dynamicObject.One = 1;
            dynamicObject.Two = 2;

            json = dynamicObject.Serialize();
            JsonTestHelper.AssertJsonEqual("{\"One\":1,\"Two\":2}", json);
        }

        [Fact]
        public static void JsonTypes_Deserialize()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            Assert.IsType<JsonObject>(JsonSerializer.Deserialize<JsonObject>("{}", options));
            Assert.IsType<JsonObject>(JsonSerializer.Deserialize<object>("{}", options));
            Assert.IsType<JsonObject>(JsonSerializer.Deserialize<JsonNode>("{}", options));
            Assert.IsType<JsonArray>(JsonSerializer.Deserialize<JsonArray>("[]", options));
            Assert.IsType<JsonArray>(JsonSerializer.Deserialize<object>("[]", options));
            Assert.IsType<JsonArray>(JsonSerializer.Deserialize<JsonNode>("[]", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<object>("true", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<object>("true", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("true", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonValue>("0", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<object>("0", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("0", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonValue>("1.2", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<object>("1.2", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("1.2", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonValue>("\"str\"", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<object>("\"str\"", options));
            Assert.IsType<JsonValue<JsonElement>>(JsonSerializer.Deserialize<JsonNode>("\"str\"", options));
        }

        /// <summary>
        /// Use a mutable DOM with the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_UsingDynamicKeyword_WithClrPrimitives()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // Change some primitives.
            obj.MyString = "Hello!";
            obj.MyBoolean = false;
            obj.MyInt = 43;

            // Add nested objects.
            // Use JsonObject; ExpandoObject should not be used since it doesn't have the same semantics including
            // null handling and case-sensitivity that respects JsonSerializerOptions.PropertyNameCaseInsensitive.
            dynamic myObject = new JsonObject(options);
            myObject.MyString = "Hello!!";
            obj.MyObject = myObject;

            dynamic child = new JsonObject(options);
            child.ChildProp = 1;
            obj.Child = child;

            // Modify number elements.
            dynamic arr = obj.MyArray;
            arr[0] = (int)arr[0] + 1;
            arr[1] = (int)arr[1] + 1;

            // Add an element.
            arr.Add(42);

            string json = obj.Serialize();
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        /// <summary>
        /// Use a mutable DOM with the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_UsingDynamicKeyword_WithJsonNode()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            dynamic obj = JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // Change some primitives.
            obj.MyString = new JsonValue<string>("Hello!");
            obj.MyBoolean = new JsonValue<bool>(false);
            obj.MyInt = new JsonValue<int>(43);

            // Add nested objects.
            // Use JsonObject; ExpandoObject should not be used since it doesn't have the same semantics including
            // null handling and case-sensitivity that respects JsonSerializerOptions.PropertyNameCaseInsensitive.
            dynamic myObject = new JsonObject(options);
            myObject.MyString = new JsonValue<string>("Hello!!");
            obj.MyObject = myObject;

            dynamic child = new JsonObject(options);
            child.ChildProp = 1;
            obj.Child = child;

            // Modify number elements.
            dynamic arr = obj.MyArray;
            arr[0] = new JsonValue<int>((int)arr[0] + 1);
            arr[1] = new JsonValue<int>((int)arr[1] + 1);

            // Add an element.
            arr.Add(new JsonValue<int>(42));

            string json = obj.Serialize();
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        [Fact]
        public static void DynamicObject_CaseSensitivity()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            dynamic obj = JsonSerializer.Deserialize<object>("{\"MyProperty\":42}", options);

            Assert.IsType<JsonObject>(obj);
            Assert.IsType<JsonValue<JsonElement>>(obj.MyProperty);

            //dynamic temp = obj.MyProperty;
            //int sdfsfd = temp;
            int sdfsfdsdf = (int)obj.MyProperty;

            var sdf = obj.MyProperty;

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Null(obj.myProperty);
            Assert.Null(obj.MYPROPERTY);

            options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
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
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            dynamic obj = JsonSerializer.Deserialize<object>("{}", options);
            Assert.Equal(null, obj.NonExistingProperty);
        }

        private class BlogPost
        {
            public string Title { get; set; }
            public string AuthorName { get; set; }
            public string AuthorTwitter { get; set; }
            public string Body { get; set; }
            public DateTime PostedDate { get; set; }
        }

        [Fact]
        public static void DynamicObject_LINQ_Convert()
        {
            string json = @"
            [
              {
                ""Title"": ""TITLE."",
                ""Author"":
                {
                  ""Name"": ""NAME."",
                  ""Mail"": ""MAIL."",
                  ""Picture"": ""/PICTURE.png""
                },
                ""Date"": ""2021-01-20T19:30:00"",
                ""BodyHtml"": ""Content.""
              }
            ]";

            JsonArray arr = JsonSerializer.Deserialize<JsonArray>(json);

            // Convert nested JSON to a flat POCO.
            IList<BlogPost> blogPosts = arr.Select(p => new BlogPost
            {
                Title = p["Title"].GetValue<string>(),
                AuthorName = p["Author"]["Name"].GetValue<string>(),
                AuthorTwitter = p["Author"]["Mail"].GetValue<string>(),
                PostedDate = p["Date"].GetValue<DateTime>(),
                Body = p["BodyHtml"].GetValue<string>()
            }).ToList();

            const string expected = "[{\"Title\":\"TITLE.\",\"AuthorName\":\"NAME.\",\"AuthorTwitter\":\"MAIL.\",\"Body\":\"Content.\",\"PostedDate\":\"2021-01-20T19:30:00\"}]";

            string json_out = JsonSerializer.Serialize(blogPosts);
            Assert.Equal(expected, json_out);
        }

        const string Linq_Query_Json = @"
        [
          {
            ""OrderId"":100, ""Customer"":
            {
              ""Name"":""Customer1"",
              ""City"":""Fargo""
            }
          },
          {
            ""OrderId"":200, ""Customer"":
            {
              ""Name"":""Customer2"",
              ""City"":""Redmond""
            }
          },
          {
            ""OrderId"":300, ""Customer"":
            {
              ""Name"":""Customer3"",
              ""City"":""Fargo""
            }
          }
        ]";

        [Fact]
        public static void DynamicObject_LINQ_Query()
        {
            JsonArray allOrders = JsonSerializer.Deserialize<JsonArray>(Linq_Query_Json);
            IEnumerable<JsonNode> orders = allOrders.Where(o => o["Customer"]["City"].GetValue<string>() == "Fargo");

            Assert.Equal(2, orders.Count());
            Assert.Equal(100, orders.ElementAt(0)["OrderId"].GetValue<int>());
            Assert.Equal(300, orders.ElementAt(1)["OrderId"].GetValue<int>());
            Assert.Equal("Customer1", orders.ElementAt(0)["Customer"]["Name"].GetValue<string>());
            Assert.Equal("Customer3", orders.ElementAt(1)["Customer"]["Name"].GetValue<string>());
        }

        [Fact]
        public static void DynamicObject_LINQ_Query_dynamic()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            IEnumerable<dynamic> allOrders = JsonSerializer.Deserialize<IEnumerable<dynamic>>(Linq_Query_Json, options);
            IEnumerable<dynamic> orders = allOrders.Where(o => ((string)o.Customer.City) == "Fargo");

            Assert.Equal(2, orders.Count());
            Assert.Equal(100, (int)orders.ElementAt(0).OrderId);
            Assert.Equal(300, (int)orders.ElementAt(1).OrderId);
            Assert.Equal("Customer1", (string)orders.ElementAt(0).Customer.Name);
            Assert.Equal("Customer3", (string)orders.ElementAt(1).Customer.Name);

            // Verify methods can be called as well.
            Assert.Equal(100, orders.ElementAt(0).OrderId.GetValue<int>());
            Assert.Equal(300, orders.ElementAt(1).OrderId.GetValue<int>());
            Assert.Equal("Customer1", orders.ElementAt(0).Customer.Name.GetValue<string>());
            Assert.Equal("Customer3", orders.ElementAt(1).Customer.Name.GetValue<string>());
        }
    }
}
#endif
