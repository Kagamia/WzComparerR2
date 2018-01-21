using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Text
{
    public class Parser
    {
        private Parser()
        {
        }

        public static IList<DocElement> Parse(string format)
        {
            var elements = new List<DocElement>();
            var sb = new StringBuilder();
            var colorStack = new Stack<string>();
            colorStack.Push("");

            int strPos = 0;
            char curChar;

            int offset = 0;

            Action flushRun = () =>
            {
                if (offset < format.Length && sb.Length > offset)
                {
                    elements.Add(new Span()
                    {
                        Text = sb.ToString(offset, sb.Length - offset),
                        ColorID = colorStack.Peek()
                    });
                    offset = sb.Length;
                }
            };

            while (strPos < format.Length)
            {
                curChar = format[strPos++];
                if (curChar == '\\')
                {
                    if (strPos < format.Length)
                    {
                        curChar = format[strPos++];
                        switch (curChar)
                        {
                            case 'r': curChar = '\r'; break;
                            case 'n': curChar = '\n'; break;
                        }
                    }
                    else //结束符处理
                    {
                        curChar = '#';
                    }
                }

                switch (curChar)
                {
                    case '#':
                        if (strPos < format.Length && format[strPos] == 'c')//遇到#c 换橙刷子并flush
                        {
                            flushRun();
                            colorStack.Push("c");
                            strPos++;
                        }
                        else if (strPos < format.Length && format[strPos] == 'g')//遇到#g(自定义) 换绿刷子并flush
                        {
                            flushRun();
                            colorStack.Push("g");
                            strPos++;
                        }
                        else if (strPos < format.Length && format[strPos] == '$')//遇到#$(自定义) 换青色刷子并flush
                        {
                            flushRun();
                            colorStack.Push("$");
                            strPos++;
                        }
                        else if (colorStack.Count == 1) //同#c
                        {
                            flushRun();
                            colorStack.Push("c");
                            //strPos++;
                        }
                        else//遇到# 换白刷子并flush
                        {
                            flushRun();
                            colorStack.Pop();
                        }
                        break;

                    case '\r': //忽略
                        break;

                    case '\n': //插入换行
                        flushRun();
                        elements.Add(LineBreak.Instance);
                        break;

                    default:
                        sb.Append(curChar);
                        break;
                }
            }

            flushRun();
            return elements;
        }
    }
}
