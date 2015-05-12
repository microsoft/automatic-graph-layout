using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpKit.JavaScript;

namespace System
{
    public static class ExtensionsForSharpKit
    {
        // Filippo: replaces unsupported Trim(char[]) function, used in DotReader
        public static string Trim_SharpKit(this string me, params char[] chars)
        {
            int start = 0, end = me.Length - 1;
            for (; start < me.Length; start++)
            {
                bool found = false;
                for (int i = 0; i < chars.Length && !found; i++)
                    if (me[start] == chars[i])
                        found = true;
                if (!found)
                    break;
            }
            for (; end >= 0; end--)
            {
                bool found = false;
                for (int i = 0; i < chars.Length && !found; i++)
                    if (me[end] == chars[i])
                        found = true;
                if (!found)
                    break;
            }
            return me.Substring(start, end - start + 1);
        }
    }
}

namespace SharpKitExtensions
{
    public class Regex_SharpKit
    {
        private JsRegExp m_JSRegex;

        public Regex_SharpKit(string pattern)
        {
            m_JSRegex = new JsRegExp(pattern);
        }

        public Match_SharpKit Match(string input, int start)
        {
            input = input.Substring(start);
            JsRegExpResult result = m_JSRegex.exec(input);
            Match_SharpKit ret = new Match_SharpKit(result);
            return ret;
        }
    }

    public class Match_SharpKit
    {
        private JsRegExpResult m_JSResult;

        public Match_SharpKit(JsRegExpResult res)
        {
            m_JSResult = res;
        }

        public bool Success { get { return m_JSResult != null; } }
        public string Value { get { return m_JSResult[0]; } }
        public int Length { get { return m_JSResult[0].length; } }
    }

    public class StringWriter_SharpKit
    {
        private StringBuilder m_SB;

        public StringWriter_SharpKit()
        {
            m_SB = new StringBuilder();
        }

        public void WriteLine(string str)
        {
            m_SB.Append(str);
            m_SB.Append("\n");
        }

        public void Flush() { }

        public void Close() { }

        public void Write(string str)
        {
            m_SB.Append(str);
        }

        public override string ToString()
        {
            return m_SB.ToString();
        }
    }
}

namespace Test
{
    public static class Test
    {
        public enum TestEnum
        {
            C = 0, D = 1
        }
        public class TestInner
        {
            public enum TestInnerEnum
            {
                A = 0, B = 1
            }
        }

        public static void RunTest()
        {
            Console.Write(TestInner.TestInnerEnum.A);
        }
    }
}