using System;
using System.Globalization;
using System.Text;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Converts TextAsset JSON content into a WZ-style <see cref="Wz_Node"/> tree.
    /// <para>
    /// object → child nodes, array → child nodes named by index, string → <see cref="string"/>,
    /// integer → <see cref="int"/> or <see cref="long"/>, values with a fraction/exponent → <see cref="double"/>,
    /// true/false → 1/0 (WZ has no Boolean type), null → node with a null value.
    /// </para>
    /// </summary>
    public static class UnityJson
    {
        private const int MaxDepth = 256;
        private const int MaxLength = 512 * 1024 * 1024;

        /// <summary>Parses a JSON document and returns a root node named <paramref name="rootName"/>.</summary>
        public static Wz_Node Parse(byte[] utf8Json, string rootName)
        {
            if (rootName == null)
            {
                throw new ArgumentNullException(nameof(rootName));
            }
            Wz_Node root = new Wz_Node(rootName);
            Parser parser = new Parser(Decode(utf8Json));
            parser.ReadRoot(root);
            return root;
        }

        /// <summary>Parses JSON and attaches the properties of the root object (or the elements of the root array) under <paramref name="parent"/>.</summary>
        public static void Populate(Wz_Node parent, byte[] utf8Json)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            Parser parser = new Parser(Decode(utf8Json));
            parser.ReadRoot(parent);
        }

        private static string Decode(byte[] utf8Json)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }
            if (utf8Json.Length > MaxLength)
            {
                throw new UnityException("The JSON is too large: " + utf8Json.Length + " bytes");
            }
            string text = Encoding.UTF8.GetString(utf8Json);
            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }
            return text;
        }

        private sealed class Parser
        {
            private readonly string text;
            private int index;

            public Parser(string text)
            {
                this.text = text;
            }

            public void ReadRoot(Wz_Node root)
            {
                this.SkipWhitespace();
                if (this.End)
                {
                    throw new UnityException("Failed to parse the JSON: the content is empty.");
                }
                this.ParseValue(root, 0);
                this.SkipWhitespace();
                if (!this.End)
                {
                    throw this.Error("There is unparsed content after the root value.");
                }
            }

            private bool End
            {
                get { return this.index >= this.text.Length; }
            }

            private char Peek()
            {
                return this.index < this.text.Length ? this.text[this.index] : '\0';
            }

            private UnityException Error(string message)
            {
                return new UnityException(string.Format(
                    "Failed to parse the JSON: {0} (offset {1})", message, this.index));
            }

            private void SkipWhitespace()
            {
                while (this.index < this.text.Length)
                {
                    char c = this.text[this.index];
                    if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                    {
                        return;
                    }
                    this.index++;
                }
            }

            private void Expect(char expected)
            {
                this.SkipWhitespace();
                if (this.Peek() != expected)
                {
                    throw this.Error(string.Format("Expected '{0}' but found '{1}'", expected, this.Peek()));
                }
                this.index++;
            }

            private void ParseValue(Wz_Node node, int depth)
            {
                if (depth > MaxDepth)
                {
                    throw this.Error("The nesting level is too deep.");
                }
                this.SkipWhitespace();
                if (this.End)
                {
                    throw this.Error("The value is incomplete.");
                }

                char c = this.Peek();
                switch (c)
                {
                    case '{':
                        this.ParseObject(node, depth);
                        break;
                    case '[':
                        this.ParseArray(node, depth);
                        break;
                    case '"':
                        node.Value = this.ReadString();
                        break;
                    case 't':
                        this.ReadLiteral("true");
                        node.Value = 1;
                        break;
                    case 'f':
                        this.ReadLiteral("false");
                        node.Value = 0;
                        break;
                    case 'n':
                        this.ReadLiteral("null");
                        node.Value = null;
                        break;
                    default:
                        node.Value = this.ReadNumber();
                        break;
                }
            }

            private void ParseObject(Wz_Node parent, int depth)
            {
                this.Expect('{');
                this.SkipWhitespace();
                if (this.Peek() == '}')
                {
                    this.index++;
                    return;
                }

                while (true)
                {
                    this.SkipWhitespace();
                    string name = this.ReadString();
                    this.Expect(':');
                    Wz_Node child = parent.Nodes.Add(name);
                    this.ParseValue(child, depth + 1);
                    this.SkipWhitespace();
                    char c = this.Peek();
                    if (c == ',')
                    {
                        this.index++;
                        continue;
                    }
                    if (c == '}')
                    {
                        this.index++;
                        return;
                    }
                    throw this.Error("Missing ',' or '}' between object properties.");
                }
            }

            private void ParseArray(Wz_Node parent, int depth)
            {
                this.Expect('[');
                this.SkipWhitespace();
                if (this.Peek() == ']')
                {
                    this.index++;
                    return;
                }

                int elementIndex = 0;
                while (true)
                {
                    Wz_Node child = parent.Nodes.Add(
                        elementIndex.ToString(CultureInfo.InvariantCulture));
                    this.ParseValue(child, depth + 1);
                    elementIndex++;
                    this.SkipWhitespace();
                    char c = this.Peek();
                    if (c == ',')
                    {
                        this.index++;
                        continue;
                    }
                    if (c == ']')
                    {
                        this.index++;
                        return;
                    }
                    throw this.Error("Missing ',' or ']' between array elements.");
                }
            }

            private string ReadString()
            {
                this.Expect('"');
                StringBuilder builder = new StringBuilder();
                while (this.index < this.text.Length)
                {
                    char c = this.text[this.index++];
                    if (c == '"')
                    {
                        return builder.ToString();
                    }
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (this.index >= this.text.Length)
                    {
                        throw this.Error("The string escape sequence is incomplete.");
                    }
                    char escape = this.text[this.index++];
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (this.index > this.text.Length - 4)
                            {
                                throw this.Error("The \\u escape sequence is incomplete.");
                            }
                            int code;
                            if (!int.TryParse(
                                this.text.Substring(this.index, 4),
                                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            {
                                throw this.Error("The \\u escape sequence is not valid hexadecimal.");
                            }
                            builder.Append((char)code);
                            this.index += 4;
                            break;
                        default:
                            throw this.Error("Unknown escape character '\\" + escape + "'");
                    }
                }
                throw this.Error("The string is not terminated.");
            }

            private object ReadNumber()
            {
                int start = this.index;
                bool isReal = false;
                while (this.index < this.text.Length)
                {
                    char c = this.text[this.index];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+')
                    {
                        this.index++;
                    }
                    else if (c == '.' || c == 'e' || c == 'E')
                    {
                        isReal = true;
                        this.index++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (this.index == start)
                {
                    throw this.Error("Unrecognized value.");
                }

                string token = this.text.Substring(start, this.index - start);
                if (!isReal)
                {
                    long integer;
                    if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
                    {
                        if (integer >= int.MinValue && integer <= int.MaxValue)
                        {
                            return (int)integer;
                        }
                        return integer;
                    }
                }

                double real;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
                {
                    throw this.Error("Invalid number '" + token + "'");
                }
                return real;
            }

            private void ReadLiteral(string literal)
            {
                if (this.index > this.text.Length - literal.Length
                    || string.CompareOrdinal(this.text, this.index, literal, 0, literal.Length) != 0)
                {
                    throw this.Error("Expected '" + literal + "'");
                }
                this.index += literal.Length;
            }
        }
    }
}
