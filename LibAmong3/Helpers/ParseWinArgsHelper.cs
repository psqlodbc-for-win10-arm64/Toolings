using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class ParseWinArgsHelper
    {
        public IEnumerable<string> ParseArgs(string input)
        {
            var list = new List<string>();
            for (int x = 0, cx = input.Length; x < cx;)
            {
                if (char.IsWhiteSpace(input[x]))
                {
                    ++x;
                    continue;
                }

                // abc → `abc`
                // "abc" → `abc`
                // "a""b""c""" → `a"b"c`
                // /I"A B C" → `/IA B C`
                // "" → `` (valid empty arg)
                // """" → `"`

                var arg = "";
                var inQuote = false;

                while (x < cx)
                {
                    if (input[x] == '"')
                    {
                        int numQuoteMarks = 1;
                        ++x;
                        while (x < cx && input[x] == '"')
                        {
                            ++x;
                            ++numQuoteMarks;
                        }

                        if (inQuote)
                        {
                            // "text"
                            // "text""
                            // "text"""
                            // "text""""
                            // "text"""""
                            // "text""""""

                            // If 1, emit 0 '"' and exit inQuote
                            // If 2, emit 1 '"' and continue inQuote
                            // If 3, emit 1 '"' and exit inQuote
                            // If 4, emit 2 '"' and continue inQuote
                            // If 5, emit 2 '"' and exit inQuote
                            // If 6, emit 3 '"' and continue inQuote

                            arg += new string('"', numQuoteMarks / 2);
                            if ((numQuoteMarks & 1) != 0)
                            {
                                inQuote = false;
                                continue;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // text"
                            // text""
                            // text"""
                            // text""""
                            // text"""""
                            // text""""""

                            // If 1, emit 0 '"' and enter inQuote
                            // If 2, emit 0 '"' and discontinue inQuote
                            // If 3, emit 1 '"' and enter inQuote
                            // If 4, emit 1 '"' and discontinue inQuote
                            // If 5, emit 2 '"' and enter inQuote
                            // If 6, emit 2 '"' and discontinue inQuote

                            arg += new string('"', (numQuoteMarks - 1) / 2);
                            if ((numQuoteMarks & 1) != 0)
                            {
                                inQuote = true;
                                continue;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (!inQuote && char.IsWhiteSpace(input[x]))
                    {
                        ++x;
                        break;
                    }
                    else
                    {
                        arg += input[x];
                        ++x;
                        continue;
                    }
                }

                list.Add(arg);
            }
            return list.AsReadOnly();
        }
    }
}
