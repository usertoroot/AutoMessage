using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoMessage.Extensions
{
    public static class StringExtensions
    {
        public static string[] ContextAwareSplit(this string text, string delimiter, char contextUp, char contextDown, bool removeEmptyEntries)
        {
            var tokens = new List<string>();
            var contextDepth = 0;
            var tokenBuilder = new StringBuilder(256);

            for (var i = 0; i < text.Length;)
            {
                char c = text[i];

                if (text[i] == contextUp)
                    contextDepth++;
                else if (text[i] == contextDown)
                    contextDepth--;
                else if (i + delimiter.Length <= text.Length && text.Substring(i, delimiter.Length) == delimiter && contextDepth == 0)
                {
                    tokens.Add(tokenBuilder.ToString());
                    tokenBuilder.Clear();
                    i += delimiter.Length;
                    continue;
                }

                tokenBuilder.Append(c);
                i++;
            }

            tokens.Add(tokenBuilder.ToString());

            if (removeEmptyEntries)
                return tokens.Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

            return tokens.ToArray();
        }
    }
}
