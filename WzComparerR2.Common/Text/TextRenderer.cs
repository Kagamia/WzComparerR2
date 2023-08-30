using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace WzComparerR2.Text
{
    public abstract class TextRenderer<TFont>
    {
        public TextRenderer()
        {
            sb = new StringBuilder();
        }

        public bool WordWrapEnabled { get; set; }

        const int MAX_RANGES = 32;

        protected StringBuilder sb;
        protected TFont font;

        public void DrawFormatString(string s, TFont font, int width, ref int y, int height)
        {
            //初始化环境
            this.font = font;
            this.sb.Clear();
            this.sb.EnsureCapacity(s.Length);

            //读取格式
            var doc = Parser.Parse(s);
            var runs = PrepareRuns(doc);

            //拆分成词
            runs = runs.SelectMany(run => SplitWords(run)).ToList();

            //对词进行measure
            MeasureRuns(runs);

            //直接绘制
            DrawRuns(runs, width, ref y, height);
        }

        public void DrawPlainText(string s, TFont font, int width, ref int y, int height)
        {
            this.font = font;
            this.sb.Clear();
            this.sb.EnsureCapacity(s.Length);

            var runs = PrepareRuns(s);
            runs = runs.SelectMany(run => SplitWords(run)).ToList();
            MeasureRuns(runs);
            DrawRuns(runs, width, ref y, height);
        }

        private List<Run> PrepareRuns(IList<DocElement> doc)
        {
            var runs = new List<Run>();
            foreach (var elem in doc)
            {
                if (elem is Span)
                {
                    var span = (Span)elem;
                    int start = sb.Length;
                    sb.Append(span.Text);
                    runs.Add(new Run(start, sb.Length - start) { ColorID = span.ColorID });
                }
                else if (elem is LineBreak)
                {
                    runs.Add(new Run(sb.Length, 0) { IsBreakLine = true });
                }
            }
            return runs;
        }

        private List<Run> PrepareRuns(string text)
        {
            List<Run> runs = new List<Run>();
            var sr = new System.IO.StringReader(text);
            for (int row = 0; sr.Peek() > -1; row++)
            {
                if (row > 0)
                {
                    runs.Add(new Run(sb.Length, 0) { IsBreakLine = true });
                }
                var line = sr.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    sb.Append(line);
                    runs.Add(new Run(sb.Length - line.Length, line.Length));
                }
            }
            return runs;
        }

        private List<Run> SplitWords(Run run)
        {
            List<Run> runs = new List<Run>();

            if (run.IsBreakLine)
            {
                runs.Add(run);
            }
            else
            {
                for (int i = run.StartIndex, i0 = run.StartIndex + run.Length; i < i0; i++)
                {
                    int start = i, len;
                    switch (sb[i])
                    {
                        case ' ':
                        case '\t':
                            while (++i < i0)
                            {
                                if (!(sb[i] == ' ' || sb[i] == '\t'))
                                {
                                    break;
                                }
                            }
                            len = (i--) - start;
                            runs.Add(new Run(start, len) { IsWhiteSpace = true });
                            break;

                        case '\r':
                            if (i + 1 < i0 && sb[i + 1] == '\n')
                            {
                                i++;
                                goto case '\n';
                            }
                            else
                            {
                                runs.Add(new Run(start, 1) { IsWhiteSpace = true });
                            }
                            break;

                        case '\n':
                            len = i - start + 1;
                            runs.Add(new Run(start, len) { IsBreakLine = true });
                            break;

                        default:
                            if (this.WordWrapEnabled)
                            {
                                while (++i < i0)
                                {
                                    if (sb[i] == ' ' || sb[i] == '\t' || sb[i] == '\r' || sb[i] == '\n')
                                    {
                                        break;
                                    }
                                }

                                len = (i--) - start;
                                runs.Add(new Run(start, len) { ColorID = run.ColorID });
                            }
                            else
                            {
                                runs.Add(new Run(start, 1) { ColorID = run.ColorID });
                            }
                            break;
                    }
                }
            }
            return runs;
        }

        private float GetFontLineHeight(Font font)
        {
            var ff = font.FontFamily;
            return (float)Math.Ceiling(1.0 * font.Height * ff.GetLineSpacing(font.Style) / ff.GetEmHeight(font.Style));
        }

        protected abstract void MeasureRuns(List<Run> runs);

        protected abstract Rectangle[] MeasureChars(int startIndex, int length);

        protected abstract void Flush(StringBuilder sb, int startIndex, int length, int x, int y, string ColorID);

        private void DrawRuns(List<Run> runs, int width, ref int y, int lineHeight)
        {
            int drawX = 0;
            int drawY = y;
            int start = -1, end = -1;
            int xOffset = 0;

            int curX = drawX;

            Func<bool> hasContent = () => start > -1 && end > start;
            string colorID = null;

            Action<bool> flush = (isNewLine) =>
            {
                if (hasContent())
                {
                    Flush(sb, start, end - start, drawX, drawY, colorID);
                }
                if (isNewLine)
                {
                    drawX = curX = 0;
                    drawY += lineHeight;
                }
                else
                {
                    drawX = curX;
                }
                start = end = -1;
            };

            for (int r = 0; r < runs.Count; r++)
            {
                var run = runs[r];
                if (run.IsBreakLine)
                { //强行换行 并且flush
                    flush(true);
                    if (r < runs.Count - 1)
                    {
                        xOffset = runs[r + 1].X;
                    }
                }
                else
                {
                    if (!run.IsWhiteSpace && run.ColorID != colorID)
                    {
                        end = run.StartIndex;
                        curX = run.X - xOffset;
                        flush(false);
                        colorID = run.ColorID;
                    }

                    if (start < 0)
                    {
                        start = run.StartIndex;
                    }

                    if (!(run.IsWhiteSpace && run.Width <= 0))
                    { //非空 计算宽度
                        curX = run.X - xOffset;
                        if (this.WordWrapEnabled ? (width - curX < run.Width) : (curX >= width))  //奇怪的算法 暂定
                        { //宽度不够
                            if (curX > 0) //(hasContent())
                            { //有内容
                                // 判断行尾标点是否追加
                                if (run.ColorID == colorID && run.Length == 1 && ",.".IndexOf(this.sb[run.StartIndex]) > -1)
                                {
                                    end = run.StartIndex + run.Length;
                                    if (++r >= runs.Count)
                                    {
                                        break;
                                    }
                                    run = runs[r];
                                }
                                flush(true);
                                start = run.StartIndex;
                                xOffset = run.X;
                            }
                            if (width - curX < run.Width)
                            { //宽度还是不够 按字符拆分
                                var rects = MeasureChars(run.StartIndex, run.Length);

                                for (int i = 0, ir = run.StartIndex; i < rects.Length; i++, ir++)
                                {
                                    rects[i].X += run.X;

                                    if (start < 0)
                                    {
                                        start = ir;
                                        xOffset = run.X;
                                    }

                                    if (rects[i].Right - xOffset > width)
                                    { //超宽 flush之前内容
                                        if (ir - start <= 0)
                                        { //限定至少输出一个字符
                                            end = start + 1;
                                            flush(true);
                                            xOffset = rects[i].Right;
                                            continue;
                                        }
                                        else
                                        {
                                            end = ir;
                                            flush(true);
                                            start = ir;
                                            xOffset = rects[i].X;
                                        }
                                    }
                                }
                                end = run.StartIndex + run.Length;
                                curX = rects[rects.Length - 1].Right - xOffset;
                                flush(false);

                                continue;
                            }
                        }
                    }

                    //正常绘制
                    end = run.StartIndex + run.Length;

                }
            }

            //输出结尾
            flush(true);
            y = drawY;
        }
    }

    public class Run
    {
        public Run(int startIndex, int length)
        {
            this.StartIndex = startIndex;
            this.Length = length;
        }

        public int StartIndex;
        public int Length;
        public bool IsWhiteSpace;
        public bool IsBreakLine;
        public int X;
        public int Width;
        public string ColorID;
    }
}
