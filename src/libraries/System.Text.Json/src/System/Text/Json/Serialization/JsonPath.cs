// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Text.Json.Serialization
{
    internal readonly partial struct JsonPath
    {
        public readonly Element[] _elements;

        public JsonPath(string jsonPath)
        {
            if (jsonPath == null)
            {
                throw new ArgumentNullException(nameof(jsonPath));
            }

            if (!jsonPath.StartsWith("$"))
            {
                throw new ArgumentException("todo", nameof(jsonPath));
            }

            _elements = null!;
            _elements = new Element[GetLevelCount(jsonPath)];

            int levels = 0;

            for (int i = 0; i < jsonPath.Length; i++)
            {
                if (jsonPath[i] == '.')
                {
                    if (i + 1 < jsonPath.Length)
                    {
                        if (jsonPath[i + 1] == '[')
                        {
                            // Treat ".[" as a single element.
                            i++;
                            _elements[levels++] = CreatePropertyNameElement_Subscript(jsonPath, ref i);
                        }
                        else
                        {
                            _elements[levels++] = CreatePropertyNameElement_DotChild(jsonPath, ref i);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("todo", nameof(jsonPath));
                    }
                }
                else if (jsonPath[i] == '[')
                {
                    if (i + 1 < jsonPath.Length)
                    {
                        i++;
                        if (jsonPath[i] == '\'')
                        {
                            _elements[levels++] = CreatePropertyNameElement_Subscript(jsonPath, ref i);
                        }
                        else
                        {
                            _elements[levels++] = CreateArrayIndexElement(jsonPath, ref i);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("todo", nameof(jsonPath));
                    }
                }
                // else validate + skip to next token
            }

            Debug.Assert(levels == _elements.Length);
        }

        private Element CreatePropertyNameElement_Subscript(string jsonPath, ref int i)
        {
            int iStart = i;
            for (; i < jsonPath.Length; i++)
            {
                if (jsonPath[i] == '\'')
                {
                    string name = jsonPath.Substring(iStart, i - iStart - 1);
                    if (name.Length > 0)
                    {
                        return new Element() { Name = name, StringWithSubscript = true };
                    }

                    throw new ArgumentException("todo", nameof(jsonPath));
                }
                // else if IsInvalidCharacter then throw
            }

            throw new ArgumentException("todo", nameof(jsonPath));
        }

        private Element CreateArrayIndexElement(string jsonPath, ref int i)
        {
            int iStart = i;
            for (; i < jsonPath.Length; i++)
            {
                if (jsonPath[i] == ']')
                {
                    if (int.TryParse(
                        jsonPath.AsSpan(iStart, i - iStart - 1),
                        NumberStyles.Integer,
                        NumberFormatInfo.InvariantInfo,
                        out int arrayIndex))
                    {
                        return new Element() { Index = arrayIndex };
                    }
                    else
                    {
                        throw new ArgumentException("todo", nameof(jsonPath));
                    }
                }
                // else if IsInvalidCharacter then throw
            }

            throw new ArgumentException("todo", nameof(jsonPath));
        }

        private Element CreatePropertyNameElement_DotChild(string jsonPath, ref int i)
        {
            int iStart = i;
            for (; i < jsonPath.Length; i++)
            {
                if (jsonPath[i] == '.' || jsonPath[i] == '[')
                {
                    string name = jsonPath.Substring(iStart, i - iStart - 1);
                    if (name.Length > 0)
                    {
                        return new Element() { Name = name };
                    }

                    throw new ArgumentException("todo", nameof(jsonPath));
                }
                // else if IsInvalidCharacter then throw
            }

            throw new ArgumentException("todo", nameof(jsonPath));
        }

        // Get level count; validation occurs later
        private int GetLevelCount(string jsonPath)
        {
            int levels = 0;
            for (int i = 0; i < jsonPath.Length; i++)
            {
                if (jsonPath[i] == '.')
                {
                    if (i + 1 < jsonPath.Length)
                    {
                        if (jsonPath[i + 1] == '[')
                        {
                            // Treat ".[" as a single element.
                            i++;
                        }

                    }
                }
                else if (jsonPath[i] == '[')
                {
                    levels++;
                }
            }

            return levels;
        }
    }
}
