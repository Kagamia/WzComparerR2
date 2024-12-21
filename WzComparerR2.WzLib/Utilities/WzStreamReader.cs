using System;
using System.IO;
using System.Text;

namespace WzComparerR2.WzLib.Utilities
{
    internal class WzStreamReader
    {
        public WzStreamReader(Stream stream)
        {
            this.BaseStream = stream;
            this.streamReader = new StreamReader(stream, encoding: Encoding.UTF8, true, 1024, true);
            this.charBuffer = new StringBuilder(64);
        }

        public Stream BaseStream { get; private set; }
        private readonly StreamReader streamReader;
        private readonly StringBuilder charBuffer;

        public bool EndOfStream => this.streamReader.EndOfStream;
        public int Read() => this.streamReader.Read();
        public int Peek() => this.streamReader.Peek();
        public string ReadLine() => this.streamReader.ReadLine();

        public void SkipLine()
        {
            while (!this.streamReader.EndOfStream)
            {
                int nextChar = streamReader.Read();
                if (nextChar == '\n')
                {
                    break;
                }
            }
        }

        public bool SkipLineAndCheckEmpty()
        {
            bool allWhiteSpace = true;
            while (!this.streamReader.EndOfStream)
            {
                int nextChar = this.streamReader.Read();
                if (nextChar == '\n')
                {
                    break;
                }
                else if (!char.IsWhiteSpace((char)nextChar))
                {
                    allWhiteSpace = false;
                }
            }
            return allWhiteSpace;
        }

        public void SkipWhitespaceExceptLineEnding()
        {
            while (!this.streamReader.EndOfStream)
            {
                int nextChar = this.streamReader.Peek();
                if (nextChar == '\n' || !char.IsWhiteSpace((char)nextChar))
                {
                    break;
                }
                this.streamReader.Read();
            }
        }

        public string ReadUntilWhitespace()
        {
            this.charBuffer.Clear();
            while (!this.streamReader.EndOfStream)
            {
                int nextChar = this.streamReader.Peek();
                if (char.IsWhiteSpace((char)nextChar))
                {
                    break;
                }
                this.charBuffer.Append((char)this.streamReader.Read());
            }
            return this.charBuffer.ToString();
        }
    }
}
