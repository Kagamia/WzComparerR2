using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace WzComparerR2.Text
{
    public abstract class DocElement
    {
    }

    public sealed class Span : DocElement
    {
        public string ColorID { get; set; }
        public string Text { get; set; }
    }

    public sealed class LineBreak : DocElement
    {
        private LineBreak() { }
        public static readonly LineBreak Instance = new LineBreak();
    }
}
