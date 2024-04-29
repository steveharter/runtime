// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
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
                        Assert.Equal(value, new Dictionary<string, string>() {{"key1", "value1"},{"key2", "value2"}});
                    }
                }
            }

            Assert.True(exceptionThrown);
        }

        [ConditionalFact(nameof(AllowsCustomDeserializer))]
        public static void CustomDeserializer()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions["System.Resources.BinaryFormat.Deserializer"] = "System.Resources.Extensions.Tests.CustomBinaryResourceDeserializerTests+CustomResourceReader, System.Resources.Extensions.Tests";
            RemoteExecutor.Invoke(() =>
            {
                using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
                using (DeserializingResourceReader reader = new DeserializingResourceReader(resourcesStream))
                {
                    int typeCount = 0;
                    IDictionaryEnumerator dictEnum = reader.GetEnumerator();
                    while (dictEnum.MoveNext())
                    {
                        switch ((string)dictEnum.Key)
                        {
                            case "dict_string_string_bin":
                                _customDeserializerInvoked = false;
                                // Dictionary<string, string> cannot be deserialized because the type string is incorrect (need to determine why).
                                Assert.Throws<SerializationException>(() => dictEnum.Value);
                                Assert.True(_customDeserializerInvoked);
                                typeCount++;
                                break;
                            case "list_int_bin":
                                _customDeserializerInvoked = false;
                                // List<int> cannot be deserialized because the type string is incorrect (need to determine why).
                                //Assert.Equal(new List<int>() { 1, 2, 3, 4, 5, 6 }, dictEnum.Value);
                                Assert.Throws<SerializationException>(() => dictEnum.Value);
                                Assert.True(_customDeserializerInvoked);
                                typeCount++;
                                break;
                            case "point_string":
                                _customDeserializerInvoked = false;
                                Assert.Equal(new Point(2, 6), dictEnum.Value);
                                Assert.False(_customDeserializerInvoked);
                                typeCount++;
                                break;
                            case "rect_string":
                                _customDeserializerInvoked = false;
                                Assert.Equal(new Rectangle(3, 6, 10, 20), dictEnum.Value);
                                Assert.False(_customDeserializerInvoked);
                                typeCount++;
                                break;
                            case "myResourceType_bytes":
                                _customDeserializerInvoked = false;
                                Assert.Equal(new MyResourceType(new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89 }), dictEnum.Value);
                                Assert.False(_customDeserializerInvoked);
                                typeCount++;
                                break;
                        }
                    }
                    Assert.Equal(5, typeCount);
                }
            }, options).Dispose();
        }

        private class CustomResourceReader
        {
            public object? Deserialize(Stream stream, Type? type)
            {
                _customDeserializerInvoked = true;
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                if (type == typeof(Dictionary<string, string>))
                {
                    // Fails. The type string is:
                    // "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
                    // which is missing "[[System.String," from the beginning.
                    return binaryFormatter.Deserialize(stream);
                }
                else if (type == typeof(List<int>))
                {
                    // Fails. Type string is:
                    // 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]'.
                    return binaryFormatter.Deserialize(stream);
                }

                throw new NotSupportedException($"Custom deserializer callback did not expect type: {type.Name}");
            }
        }
    }
}
