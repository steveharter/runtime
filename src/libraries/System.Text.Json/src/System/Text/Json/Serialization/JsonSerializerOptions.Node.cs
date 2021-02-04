// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    /// <summary>
    /// Provides options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    public sealed partial class JsonSerializerOptions
    {
#if BUILDING_INBOX_LIBRARY
        internal bool AreDynamicTypesEnabled { get; set; }
#endif

        /// <summary>
        /// todo
        /// </summary>
        public void EnableDynamicTypes()
        {
            VerifyMutable();

#if BUILDING_INBOX_LIBRARY
            Converters.Add(new JsonDynamicNodeConverterFactory());
            AreDynamicTypesEnabled = true;
#else
            throw new NotSupportedException("Dynamic types not supported in NetStandard.");
#endif
        }
    }
}
