using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WzComparerR2
{
    public static class Calculator
    {
        public static double Parse(string mathExpression, params double[] args)
        {
            
            List<string> split = Split(mathExpression);
            PreTreat(split);
            SuffixExpression(split);
            Dictionary<string, double> argDict = null;
            if (args != null)
            {
                argDict = new Dictionary<string, double>(args.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    switch (i)
                    {
                        case 0: argDict["x"] = args[0]; break;
                        case 1: argDict["y"] = args[1]; break;
                        case 2: argDict["z"] = args[2]; break;
                        case 3: argDict["w"] = args[3]; break;
                    }
                }
            }
            double value = Calculate(split, argDict);
            return value;
        }

        private static List<string> Split(string mathExpression)
        {
            List<string> listStr = new List<string>();
            if (string.IsNullOrEmpty(mathExpression))
                return listStr;

            int begin = -1, len = 0, type = -1;
            for (int i = 0; i <= mathExpression.Length; i++)
            {
                int t = (i == mathExpression.Length) ? 0 : GetCharType(mathExpression[i]);
                if (t == 0)
                {
                    if (len != 0)
                    {
                        listStr.Add(mathExpression.Substring(begin, len));
                        len = 0;
                    }
                }
                else
                {
                    if (len > 0 && !(t == type || t + type == 9))//符号类别和当前缓存不相同
                    {
                        listStr.Add(mathExpression.Substring(begin, len));
                        len = 0;
                    }
                    if (len == 0)
                    {
                        begin = i;
                        type = t;
                    }
                    len++;
                }
            }
            return listStr;
        }

        private static void PreTreat(List<string> split)
        {
            if (split == null)
                return;
            for (int i = 0; i < split.Count; i++)
            {
                if ((split[i] == "+" || split[i] == "-")
                    && (i == 0 || split[i - 1] == "("))
                {
                    split.Insert(i++, "0");
                }
            }
        }

        private static void SuffixExpression(List<string> split)
        {
            List<string> result = new List<string>(split.Count);
            Stack<string> stack = new Stack<string>(split.Count);

            foreach (string str in split)
            {
                if (IsOperator(str) || IsFunction(str))
                {
                    if (str == "(")
                    {
                        stack.Push(str);
                    }
                    else if (str == ")")
                    {
                        while (stack.Count > 0)
                        {
                            string temp = stack.Pop();
                            if (temp == "(")
                                break;
                            else
                                result.Add(temp);
                        }
                    }
                    else
                    {
                        if (stack.Count > 0)
                        {
                            while (stack.Count > 0)
                            {
                                string temp = stack.Pop();
                                if (GetPriority(str) > GetPriority(temp))
                                {
                                    stack.Push(temp);
                                    stack.Push(str);
                                    break;
                                }
                                else
                                {
                                    result.Add(temp);
                                    if (stack.Count == 0)
                                    {
                                        stack.Push(str);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            stack.Push(str);
                        }
                    }
                }
                else
                {
                    result.Add(str);
                }
            }
            while (stack.Count > 0)
            {
                result.Add(stack.Pop());
            }

            split.Clear();
            split.AddRange(result);
        }

        private static int GetCharType(char c)
        {
            switch (c)
            {
                case '.':
                    return 1; //数字
                case '+':
                case '-':
                case '*':
                case '/':
                    return 2; //运算符
                case '(':
                case ')':
                    return 4; //括号
                default:
                    if (c >= '0' && c <= '9')
                        return 1;
                    else if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
                        return 8; //函数
                    break;
            }
            return 0; //无效字符
        }

        private static string[] operatorList = new string[] { "+", "-", "*", "/", "(", ")" };
        private static string[] functionList = new string[] { "u", "d" };

        private static bool IsOperator(string str)
        {
            foreach (string op in operatorList)
            {
                if (op == str) return true;
            }
            return false;
        }

        private static bool IsFunction(string str)
        {
            foreach (string func in functionList)
            {
                if (func == str) return true;
            }
            return false;
        }

        private static int GetPriority(string str)
        {
            switch (str)
            {
                case "+":
                case "-":
                    return 1;
                case "*":
                case "/":
                    return 2;
                case "u":
                case "d":
                    return 3;
                default:
                    return 0;
            }
        }

        private static double Calculate(List<string> split, Dictionary<string, double> args)
        {
            Stack<double> stack = new Stack<double>(split.Count);

            foreach (string str in split)
            {
                if (IsOperator(str))
                {
                    OperatorEntry(str,stack);
                }
                else if (IsFunction(str))
                {
                    FunctionEntry(str,stack);
                }
                else
                {
                    stack.Push(ParseArgValue(args, str));
                }
            }
            return stack.Pop();
        }

        private static double ParseArgValue(Dictionary<string, double> args, string arg)
        {
            double value;
            if (!double.TryParse(arg, out value))
            {
                if (args != null)
                {
                    args.TryGetValue(arg, out value);
                }
            }
            return value;
        }

        private static void FunctionEntry(string func, Stack<double> stack)
        {
            switch (func)
            {
                case "u":
                    stack.Push(Math.Ceiling(stack.Pop()));
                    break;
                case "d":
                    stack.Push(Math.Floor(stack.Pop()));
                    break;
            }
        }

        private static void OperatorEntry(string op, Stack<double> stack)
        {
            double sec = stack.Pop(), fir = stack.Pop();
            switch (op)
            {
                case "+":
                    stack.Push(fir + sec);
                    break;
                case "-":
                    stack.Push(fir - sec);
                    break;
                case "*":
                    stack.Push(fir * sec);
                    break;
                case "/":
                    stack.Push(fir / sec);
                    break;
                default:
                    stack.Push(0);
                    break;
            }
        }
    }
}
