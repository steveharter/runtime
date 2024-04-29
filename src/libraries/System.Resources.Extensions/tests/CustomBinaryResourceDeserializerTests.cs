// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Resources.Extensions.Tests
{
    public class CustomBinaryResourceDeserializerTests
    {
        public static bool _customDeserializerInvoked = false;

        public static bool AllowsCustomDeserializer
        {
            get
            {
                if (!AppContext.TryGetSwitch("System.Resources.ResourceManager.AllowCustomResourceTypes", out bool isEnabled) || isEnabled)
                {
                    return PlatformDetection.IsNetCore;
                }

                return false;
            }
        }

        [Fact]
        public static void CustomDeserializer_BadConfigEntry_Class()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();

            options.RuntimeConfigurationOptions["System.Resources.BinaryFormat.Deserializer"] = "System.Resources.Extensions.Tests.CustomBinaryResourceDeserializerTests+CustomResourceReader, BADCLASS";
            RemoteExecutor.Invoke(() =>
            {
                bool exceptionThrown = false;
                using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
                using (DeserializingResourceReader reader = new DeserializingResourceReader(resourcesStream))
                {
                    IDictionaryEnumerator dictEnum = reader.GetEnumerator();
                    while (dictEnum.MoveNext())
                    {
                        if (dictEnum.Key.Equals("dict_string_string_bin"))
                        {
                            exceptionThrown = true;
                            TypeLoadException ex = Assert.Throws<TypeLoadException>(() => dictEnum.Value);
                            Assert.Contains("BADCLASS", ex.Message);
                        }
                    }
                }

                Assert.True(exceptionThrown);
            }, options).Dispose();
        }

        [Fact]
        public static void CustomDeserializer_BadConfigEntry_Assembly()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();

            options.RuntimeConfigurationOptions["System.Resources.BinaryFormat.Deserializer"] = "BADASSEMBLY, BADCLASS";
            RemoteExecutor.Invoke(() =>
            {
                bool exceptionThrown = false;
                using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
                using (DeserializingResourceReader reader = new DeserializingResourceReader(resourcesStream))
                {
                    IDictionaryEnumerator dictEnum = reader.GetEnumerator();
                    while (dictEnum.MoveNext())
                    {
                        if (dictEnum.Key.Equals("dict_string_string_bin"))
                        {
                            exceptionThrown = true;
                            TypeLoadException ex = Assert.Throws<TypeLoadException>(() => dictEnum.Value);
                            Assert.Contains("BADASSEMBLY", ex.Message);
                        }
                    }
                }

                Assert.True(exceptionThrown);
            }, options).Dispose();
        }

        [Fact]
        public static void CanDeserializeDictionaryWithoutCallback()
        {
            bool exceptionThrown = false;
            using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(resourcesStream))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();
                while (dictEnum.MoveNext())
                {
                    if (dictEnum.Key.Equals("dict_string_string_bin"))
                    {
                        exceptionThrown = true;
                        Dictionary<string, string> value = (Dictionary<string, string>)dictEnum.Value;
                        Assert.Equal(value, new Dictionary<string, string>() { { "key1", "value1" }, { "key2", "value2" } });
                    }
                }
            }

            Assert.True(exceptionThrown);
        }

        public static IEnumerable<object[]> BuiltInDeserializerData()
        {
            yield return new object[] { "point_string", new List<int>() { 1, 2, 3, 4, 5, 6 } };
            yield return new object[] { "rect_string", new Rectangle(3, 6, 10, 20) };
            yield return new object[] { "myResourceType_bytes", new MyResourceType(new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89 }) };
        }

        [ConditionalTheory(nameof(AllowsCustomDeserializer))]
        [MemberData(nameof(BuiltInDeserializerData))]
        public static void CustomDeserializer_BuiltInDeserialize(string key, object expected)
        {
            // We set up the custom deserializer, but it is not invoked for the these types.
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions["System.Resources.BinaryFormat.Deserializer"] =
                "System.Resources.Extensions.Tests.CustomBinaryResourceDeserializerTests+CustomResourceReader, System.Resources.Extensions.Tests";

            RemoteExecutor.Invoke(() =>
            {
                using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
                using (DeserializingResourceReader reader = new DeserializingResourceReader(resourcesStream))
                {
                    _customDeserializerInvoked = false;
                    Assert.Equal(expected, GetValue(key, reader));
                    Assert.False(_customDeserializerInvoked);
                }
            }, options).Dispose();
        }

        public static IEnumerable<object[]> CustomDeserializerData()
        {
            yield return new object[] { "dict_string_string_bin", new Dictionary<string, string>() { { "key1", "value1" }, { "key2", "value2" } } };
            yield return new object[] { "list_int_bin", new List<int>() { 1, 2, 3, 4, 5, 6 } };
        }

        [ConditionalTheory(nameof(AllowsCustomDeserializer))]
        [MemberData(nameof(CustomDeserializerData))]
        public static void CustomDeserializer_CustomDeserializer(string key, object expected)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions["System.Resources.BinaryFormat.Deserializer"] =
                "System.Resources.Extensions.Tests.CustomBinaryResourceDeserializerTests+CustomResourceReader, System.Resources.Extensions.Tests";

            RemoteExecutor.Invoke(() =>
            {
                using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
                using (DeserializingResourceReader reader = new DeserializingResourceReader(resourcesStream))
                {
                    _customDeserializerInvoked = false;
                    Assert.Equal(expected, GetValue(key, reader));
                    Assert.True(_customDeserializerInvoked);
                }
            }, options).Dispose();
        }

        private class CustomResourceReader
        {
            public object? Deserialize(Stream stream, Type? type)
            {
                _customDeserializerInvoked = true;
                if (type == typeof(Dictionary<string, string>))
                {
                    return GetFormatter().Deserialize(stream);
                }
                else if (type == typeof(List<int>))
                {
                    return GetFormatter().Deserialize(stream);
                }

                throw new NotSupportedException($"Custom deserializer callback did not expect type: {type.Name}");
            }

            private static BinaryFormatter GetFormatter()
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();

                Type binderType = typeof(DeserializingResourceReader).Assembly.
                    GetType("System.Resources.Extensions.DeserializingResourceReader+UndoTruncatedTypeNameSerializationBinder", throwOnError: true);

                binaryFormatter.Binder = (SerializationBinder)Activator.CreateInstance(binderType, nonPublic: true);
                return binaryFormatter;
            }
        }

        private static object GetValue(string key, DeserializingResourceReader reader)
        {
            IDictionaryEnumerator dictEnum = reader.GetEnumerator();
            while (dictEnum.MoveNext())
            {
                if ((string)dictEnum.Key == key)
                {
                    return dictEnum.Value;
                }
            }

            throw new Exception($"Key {key} not found in the resource file");
        }
    }
}
