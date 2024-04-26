// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
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
                    IDictionaryEnumerator dictEnum = reader.GetEnumerator();
                    while (dictEnum.MoveNext())
                    {
                        var k = dictEnum.Key;
                        if (k is string key && key == "dict_string_string_bin")
                        {
                            Assert.False(_customDeserializerInvoked);
                            Assert.Throws<BadImageFormatException>(() => dictEnum.Value);
                            Assert.True(_customDeserializerInvoked);
                            break;
                        }
                    }
                }
            }, options).Dispose();
        }

        private class CustomResourceReader
        {
            public object? Deserialize(Stream stream, Type? type)
            {
                Assert.True(type == typeof(Dictionary<string, string>));
                _customDeserializerInvoked = true;
                // We didn't properly deserialize; we will get BadImageFormatException.
                return null;
            }

            static ReadOnlySpan<byte> ReadAllBytes(Stream stream)
            {
                ((MemoryStream)stream).TryGetBuffer(out ArraySegment<byte> memoryStreamBuffer);
                int position = (int)stream.Position;
                // Simulate that we read the stream to its end.
                stream.Seek(0, SeekOrigin.End);
                return memoryStreamBuffer.AsSpan(position);
            }
        }
    }
}
