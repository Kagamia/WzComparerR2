using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WzComparerR2
{
    public static class Calculator
    {
        public static decimal Parse(string mathExpression, params decimal[] args)
        {
            var tokens = Lexer(mathExpression);
            var inst = Suffix(tokens);

            var paramList = new Dictionary<string, object>();

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (i)
                    {
                        case 0: paramList["x"] = args[0]; break;
                        case 1: paramList["y"] = args[1]; break;
                        case 2: paramList["z"] = args[2]; break;
                        case 3: paramList["w"] = args[3]; break;
                    }
                }
            }

            return Execute(inst, new EvalContext(paramList));
        }

        private static List<Token> Lexer(string expr)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrEmpty(expr))
                return tokens;

            int begin;
            for (int i = 0; i < expr.Length; i++)
            {
                switch (expr[i])
                {
                    case '+':
                    case '-':
                    case '*':
                    case '/': tokens.Add(new Token(TokenType.Operator, expr[i].ToString())); break;
                    case '.': tokens.Add(new Token(TokenType.Dot, null)); break;
                    case '(': tokens.Add(new Token(TokenType.BracketStart, null)); break;
                    case ')': tokens.Add(new Token(TokenType.BracketEnd, null)); break;
                    case ',': tokens.Add(new Token(TokenType.Comma, null)); break;
                    case ' ': break; //whitespace当不存在
                    case '%': break; //ignore
                    default:
                        if (char.IsDigit(expr[i]))
                        { //尽力读取number
                            begin = i;
                            bool dot = false;
                            while (++i < expr.Length)
                            {
                                if (char.IsDigit(expr[i]))
                                {
                                    //继续读
                                }
                                else if (expr[i] == '.' && !dot)
                                {
                                    dot = true;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            tokens.Add(new Token(TokenType.Number, expr.Substring(begin, i - begin)));
                            i--;
                        }
                        else if (char.IsLetter(expr[i]))
                        { //尽力读取id
                            begin = i;
                            while (++i < expr.Length)
                            {
                                if (!char.IsLetterOrDigit(expr[i]))
                                {
                                    break;
                                }
                            }
                            tokens.Add(new Token(TokenType.ID, expr.Substring(begin, i - begin)));
                            i--;
                        }
                        else if (char.IsWhiteSpace(expr[i]))
                        {
                            //空白字符跳过
                        }
                        else
                        { //无效字符
                            throw new Exception("Unknown char '" + expr[i] + "'.");
                        }
                        break;
                }
            }
            return tokens;
        }

        //suffix 逆波兰表达式
        private static List<Token> Suffix(List<Token> tokens)
        {
            var value = new List<Token>();
            var stack = new Stack<Token>();
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                switch (token.Type)
                {
                    case TokenType.BracketStart: //括号 推进stack
                        stack.Push(token);
                        if (token.Tag == Tag.Call)
                            value.Add(new Token(TokenType.CallStart, ""));
                        break;

                    case TokenType.BracketEnd: //括号结束 弹出到上一个括号
                        {
                            Token t;
                            int count = 0;
                            while ((t = stack.Pop()) != null)
                            {
                                if (t.Type != TokenType.BracketStart)
                                {
                                    value.Add(t);
                                    count++;
                                }
                                else
                                {
                                    if (t.Tag == Tag.Call)
                                        value.Add(new Token(TokenType.CallEnd, ""));
                                    break;
                                }
                            }
                        }
                        break;

                    case TokenType.Operator: //运算符
                        if (i == 0 || tokens[i - 1].Type == TokenType.BracketStart || tokens[i - 1].Type == TokenType.Operator)
                        { //独立判定一元运算符
                            token.Tag = Tag.Unary;
                        }
                        goto case TokenType.Dot;
                    case TokenType.Dot: //取成员
                        while (stack.Count > 0)
                        { //比较优先级
                            Token t = stack.Peek();
                            if (Priority(token) > Priority(t)
                                || (token.Tag == Tag.Unary && t.Tag == Tag.Unary))
                            { //优先级比上个高
                                break;
                            }
                            else
                            {
                                value.Add(stack.Pop());
                            }
                        }
                        stack.Push(token);
                        break;

                    case TokenType.ID: //预判如果后面是括号 当成函数处理
                        value.Add(token);
                        if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.BracketStart)
                        {
                            while (stack.Count > 0)
                            {
                                Token t = stack.Peek();
                                if (t.Type == TokenType.Dot)
                                {
                                    value.Add(stack.Pop());
                                }
                                else
                                {
                                    break;
                                }
                            }
                            //标记下一个括号为call
                            tokens[i + 1].Tag = Tag.Call;
                        }
                        break;

                    case TokenType.Number:
                        value.Add(token);
                        break;

                    case TokenType.Comma: //逗号 忽略好像也没事..感觉像卖萌的..
                        break;
                }
            }

            value.AddRange(stack);
            return value;
        }

        private static decimal Execute(List<Token> inst, EvalContext param)
        {
            var stack = new Stack<object>();
            object obj;
            decimal d1, d2;
            foreach (var token in inst)
            {
                switch (token.Type)
                {
                    case TokenType.Number: stack.Push(Convert.ToDecimal(token.Value)); break;
                    case TokenType.ID:
                        if (param.TryGetValue(token.Value, out obj))
                        {
                            stack.Push(obj);
                        }
                        else
                        {
                            throw new Exception("ID '" + token.Value + "' not found.");
                        }
                        break;
                    case TokenType.Operator:
                        if (token.Tag == Tag.Unary)
                        {
                            d1 = Convert.ToDecimal(stack.Pop());
                            switch (token.Value)
                            {
                                case "+": stack.Push(d1); break;
                                case "-": stack.Push(-d1); break;
                            }
                        }
                        else
                        {
                            d2 = Convert.ToDecimal(stack.Pop());
                            d1 = Convert.ToDecimal(stack.Pop());
                            switch (token.Value)
                            {
                                case "+": stack.Push(d1 + d2); break;
                                case "-": stack.Push(d1 - d2); break;
                                case "*": stack.Push(d1 * d2); break;
                                case "/": stack.Push(d1 / d2); break;
                            }
                        }
                        break;
                    case TokenType.Dot:
                        throw new NotSupportedException();
                    case TokenType.CallStart: stack.Push(TokenType.CallStart); break;
                    case TokenType.CallEnd:
                        var p = new Stack<object>();
                        while (!TokenType.CallStart.Equals(obj = stack.Pop()))
                        {
                            p.Push(obj);
                        }
                        obj = (stack.Pop() as Delegate).DynamicInvoke(p.ToArray());
                        stack.Push(obj);
                        break;
                }
            }
            return stack.Count <= 0 ? 0 : Convert.ToDecimal(stack.Pop());
        }


        private class Token
        {
            public Token(TokenType type, String value)
            {
                this.Type = type;
                this.Value = value;
            }
            public TokenType Type;
            public string Value;
            public Tag Tag;
        }

        //优先级
        private static int Priority(Token token)
        {
            if (token.Tag == Tag.Unary) return 4;
            switch (token.Value)
            {
                case "+":
                case "-": return 1;
                case "*":
                case "/": return 2;
                case ".": return 3;
                default: return 0;
            }
        }

        private enum TokenType
        {
            ID, //x,funcName
            Number, //123.45
            BracketStart, //(
            BracketEnd, //)
            Dot, //.成员运算符
            Operator, //+-*/
            Comma, //,逗号 函数参数分隔符

            CallStart = 100, //标记用 参数开始
            CallEnd, //标记用
        }

        private enum Tag
        {
            None = 0,
            Call,
            Unary,
        }

        private class EvalContext
        {
            public EvalContext()
                : this(null)
            {
            }

            public EvalContext(Dictionary<string, object> parameters)
            {
                this._dict = new Dictionary<string, object>();

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var kv in parameters)
                    {
                        _dict.Add(kv.Key, kv.Value);
                    }
                }
            }

            public Dictionary<string, object> _dict;

            public bool TryGetValue(string key, out object value)
            {
                return _dict.TryGetValue(key, out value)
                    || TryGetFunction(key, out value);
            }

            private bool TryGetFunction(string key, out object value)
            {
                Match m;

                if (key == "u")
                {
                    value = (Func<decimal, decimal>)Math.Ceiling;
                    return true;
                }
                else if (key == "d")
                {
                    value = (Func<decimal, decimal>)Math.Floor;
                    return true;
                }
                else if ((m = Regex.Match(key, @"log(\d+)")).Success)
                {
                    var logBase = int.Parse(m.Result("$1"));
                    value = (Func<decimal, decimal>)(x => x <= 0 ? 0 : (decimal)Math.Floor(Math.Log(decimal.ToDouble(x), logBase)));
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
