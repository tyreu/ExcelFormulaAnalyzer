using System.Text.RegularExpressions;

namespace ExcelFormulaAnalyzer
{
    public enum TokenType
    {
        FunctionToken,
        NumberToken,
        KeySymbol,
        CellToken,
        FormulaPart,
        Formula,
        RangeToken,
        Default
    }
    public class TokenService
    {
        public List<Token> Tokenize(string formula, int level = 0)
        {
            var result = new List<Token>();
            var tokens = SplitByTopLevelOperators(formula, level).Where(t => t.Type != TokenType.Default);

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.CellToken ||
                    token.Type == TokenType.NumberToken ||
                    token.Type == TokenType.RangeToken)
                {
                    if (token.Type == TokenType.RangeToken)
                    {
                        result.Add(token);
                        result.AddRange(RangeParser.GetCellsFromRange(token.Value).Select(t => new Token
                        {
                            Level = token.Level + 1,
                            Value = t,
                            Type = TokenType.CellToken
                        }).ToList());
                    }
                    else
                    {
                        result.Add(token);
                    }
                }
                else if (token.Type == TokenType.FunctionToken)
                {
                    result.Add(token);
                    // Process function arguments recursively
                    var args = GetFunctionArguments(token.Value);
                    foreach (var arg in args)
                    {
                        var argTokenType = DetermineTokenType(arg);
                        if (argTokenType != TokenType.Default)
                        {
                            if (argTokenType != TokenType.CellToken &&
                                argTokenType != TokenType.RangeToken &&
                                argTokenType != TokenType.FunctionToken)
                            {
                                result.Add(new Token
                                {
                                    Value = arg,
                                    Type = argTokenType,
                                    Level = level + 1,
                                });
                                result.AddRange(Tokenize(arg, level + 2));
                            }
                            else
                                result.AddRange(Tokenize(arg, level + 1));
                        }
                    }
                }
                else if (token.Type == TokenType.FormulaPart)
                {
                    result.Add(token);
                    // Recursively process the token
                    result.AddRange(Tokenize(token.Value, level + 1));
                }
                else
                {
                    // Output symbols as they are
                    result.Add(token);
                }
            }
            return result;
        }

        private List<Token> SplitByTopLevelOperators(string formula, int level = 0)
        {
            var result = TrySplitByOperators(formula, new[] { '+', '-' }, level);

            if (result.Count == 1)  // Если разделение по + или - не сработало
            {
                result = TrySplitByOperators(result[0].Value, new[] { '*' }, level);
            }
            if (result.Count == 1)  // Если разделение по + или - не сработало
            {
                result = TrySplitByOperators(result[0].Value, new[] { '/' }, level, true);
            }

            if (result.Count == 1)  // Если разделение по / не сработало
            {
                result = TrySplitByOperators(result[0].Value, new[] { '^' }, level, true);
            }
            return result;
        }

        private List<Token> TrySplitByOperators(string formula, char[] operators, int level = 0, bool reverse = false)
        {
            var result = new List<Token>();
            int depth = 0;
            int lastSplit = reverse ? formula.Length : 0;

            int start = reverse ? formula.Length - 1 : 0;
            int end = reverse ? -1 : formula.Length;
            int step = reverse ? -1 : 1;

            for (int i = start; i != end; i += step)
            {
                if (formula[i] == '(') depth++;
                if (formula[i] == ')') depth--;

                if (depth == 0 && operators.Contains(formula[i]))
                {
                    string part1, part2;

                    part2 = formula.Substring(i + 1).Trim();
                    part1 = formula.Substring(0, i).Trim();
                    if (reverse)
                    {
                        if (part1 == "1")
                        {
                            result.Add(new Token
                            {
                                Value = RemoveOuterParentheses(part2.Trim()),
                                Type = DetermineTokenType(RemoveOuterParentheses(part2.Trim())),
                                Level = level
                            });
                            return result;
                        }
                        else
                        {
                            var part2Value = $"{(formula[i] == '/' ? "1/" : "")}{part2}";
                            result.Insert(0, new Token
                            {
                                Value = RemoveOuterParentheses(part2Value),
                                Type = DetermineTokenType($"{RemoveOuterParentheses(part2Value)}"),
                                Level = level
                            });

                            result.Insert(0, new Token
                            {
                                Value = RemoveOuterParentheses(part1),
                                Type = DetermineTokenType(RemoveOuterParentheses(part1)),
                                Level = level
                            });
                            return result;
                        }
                    }
                    else
                    {
                        part1 = formula.Substring(lastSplit, i - lastSplit).Trim();
                        lastSplit = i + 1;
                        // Добавляем токен слева от оператора
                        result.Add(new Token
                        {
                            Value = RemoveOuterParentheses(part1),
                            Type = DetermineTokenType(RemoveOuterParentheses(part1)),
                            Level = level
                        });

                        // Если оператор '/', следующий токен будет '1/part2'
                        if (formula[i] == '/' && lastSplit < formula.Length)
                        {
                            string partAfterDivision = formula.Substring(lastSplit).Trim();
                            result.Add(new Token
                            {
                                Value = $"1/{partAfterDivision}",
                                Type = DetermineTokenType($"1/{RemoveOuterParentheses(partAfterDivision)}"),
                                Level = level
                            });
                            return result;
                        }
                    }
                }
            }

            // Добавляем последнюю часть
            if (reverse && result.Count == 0)
            {
                string lastPart = formula.Trim();
                result.Add(new Token
                {
                    Value = lastPart,
                    Type = DetermineTokenType(RemoveOuterParentheses(lastPart)),
                    Level = level
                });
            }
            else if (!reverse && lastSplit < formula.Length)
            {
                string lastPart = formula.Substring(lastSplit).Trim();
                result.Add(new Token
                {
                    Value = lastPart,
                    Type = DetermineTokenType(RemoveOuterParentheses(lastPart)),
                    Level = level
                });
            }

            return result;
        }

        private string RemoveOuterParentheses(string input)
        {
            while (input.StartsWith("(") && input.EndsWith(")"))
            {
                input = input.Substring(1, input.Length - 2).Trim();
            }
            return input;
        }

        private List<string> GetFunctionArguments(string function)
        {
            var args = new List<string>();
            int depth = 0;
            int lastSplit = function.IndexOf('(') + 1;

            for (int i = lastSplit; i < function.Length; i++)
            {
                if (function[i] == '(') depth++;
                if (function[i] == ')') depth--;

                if (depth == 0 && function[i] == ',')
                {
                    args.Add(function.Substring(lastSplit, i - lastSplit).Trim());
                    lastSplit = i + 1;
                }
            }

            // Add the last argument
            if (lastSplit < function.Length - 1)
            {
                args.Add(function.Substring(lastSplit, function.Length - lastSplit - 1).Trim());
            }

            return args;
        }

        private TokenType DetermineTokenType(string token)
        {
            if (Regex.IsMatch(token, "^[+\\-*/^><!\\\\\\\"\"]"))
                return TokenType.Default;
            if (Regex.Match(token, @"^[A-Z]{2,}\(.*?\)(?=\s*\^|$)").Length == token.Length)
                return TokenType.FunctionToken;
            else if (Regex.IsMatch(token, @"^\d+(\.\d+)?$"))
                return TokenType.NumberToken;
            else if (Regex.IsMatch(token, @"^[+\-*/^()]+$"))
                return TokenType.KeySymbol;
            else if (Regex.IsMatch(token, @"^(?:'[^']+'!|[^!+\-*/^()\s]+!)?\$?[A-Z][A-Z]?\$?\d+:\$?[A-Z][A-Z]?\$?\d+$"))
                return TokenType.RangeToken;
            else if (Regex.IsMatch(token, @"^(?:'[^']+'!|[^!+\-*/^()\s]+!)?\$?[A-Z]+\$?\d+$"))
                return TokenType.CellToken;
            else
                return TokenType.FormulaPart;
        }
    }
}