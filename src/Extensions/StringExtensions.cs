using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoEcs.Generator.Extensions
{
    public static class StringExtensions
    {
        public static string ReplaceAll(this string input, string newValue, params string[] values)
        {

            foreach (var value in values)
            {
                input = input.Replace(value, newValue);
            }
            return input;
        }

        public static string RemoveWhitespace(this string input)
        {
            if (input == null) return null;
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static string Trim(this string s, string trimmer)
        {
            return s.Substring(0, s.Length - trimmer.Length);
        }

        public static string Extract(this string s, char start, char end)
        {
            var startId = s.IndexOf(start);
            var endId = s.IndexOf(end);
            return s.Substring(startId + 1, endId - startId - 1);
        }

        public static List<string> GetLines(this string text, string tag)
        {
            string[] lines = text.AslineArray();
            var result = new List<string>();
            foreach (var line in lines)
            {
                if (line.Contains(tag))
                {
                    result.Add(line.Split(new string[] { tag }, StringSplitOptions.RemoveEmptyEntries)[1].RemoveWhitespace());
                }
            }
            return result;
        }

        public static int FindMatchingBracket(this string expression, int index)
        {
            int i;

            // If index given is invalid and is  
            // not an opening bracket.  
            if (expression[index] != '{')
            {
                return -1;
            }

            // Stack to store opening brackets.  
            Stack st = new Stack();

            // Traverse through string starting from  
            // given index.  
            for (i = index; i < expression.Length; i++)
            {

                // If current character is an  
                // opening bracket push it in stack.  
                if (expression[i] == '{')
                {
                    st.Push((int)expression[i]);
                } // If current character is a closing  
                  // bracket, pop from stack. If stack  
                  // is empty, then this closing  
                  // bracket is required bracket.  
                else if (expression[i] == '}')
                {
                    st.Pop();
                    if (st.Count == 0)
                    {
                        return i ;
                    }
                }
            }

            // If no matching closing bracket  
            // is found.  
            return -2;
        }

        public static string[] AslineArray(this string input)
        {
            return input.Replace("\r", "").Split('\n');
        }

        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        public static string FirstCharToLower(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToLower() + input.Substring(1);
            }
        }

        public static string VariateFirstChar(this string input)
        {
            if (input.First() == '_') return "_" + input.Substring(1).VariateFirstChar();

            if (char.IsUpper(input.First()))
            {
                return input.FirstCharToLower();
            }
            else
            {
                return input.FirstCharToUpper();
            }
        }
    }
}
