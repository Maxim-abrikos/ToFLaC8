using ICSharpCode.AvalonEdit;
using System;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Compiler
{
    internal static class LeksAnalisation
    {
        static string invalidCharsPattern = @"[!@#\$%\^&\*\{\}\[\]<>\?/№а-яА-Я]+";
        static List<char> invalidChars = new List<char>() { '!', '@', '#', '$', '%', '^', '&', '*', '{', '}', '[', ']', '<', '>', '?', '/', '№', '_' };
        public static List<string> Analyze(TextEditor editor)
        {
            List<string> results = new List<string>();
            string text = editor.Text;
            string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            List<string> _errors = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                results.Add($"Строка {i + 1}:\n");
                /*List<string> errors*/
                _errors.AddRange(AnalyzeLine(lines[i]));

                if (_errors.Count == 0)
                {
                    results.Add("Ошибок нет");
                }
                else
                {
                    foreach (string error in _errors)
                    {
                        results.Add(error);
                    }
                }
            }

            return results;
        }


        static List<string> Types = new List<string>() {
    @"^[a-zA-Z_][a-zA-Z0-9_]*$",
    @"^[a-zA-Z_][a-zA-Z0-9_]*$",
    @"=",
    @"^[a-zA-Z_][a-zA-Z0-9_]*$",
    @"^[a-zA-Z_][a-zA-Z0-9_]*$",
    @"\(", // Экранированная открывающая скобка
    @"^-?\d+(\.\d+)?(?:f)?$",
    @",",
    @"^-?\d+(\.\d+)?(?:f)?$",
    @"\)", // Экранированная закрывающая скобка
    @";"
};

        //static List<string> Types = new List<string>() { @"^[a-zA-Z_]+$", @"^[a-zA-Z_]+$", @"=", @"^[a-zA-Z_]+$", @"^[a-zA-Z_]+$", @"(", @"^-?\d+(\.\d+)?f?$", @",", @"^-?\d+(\.\d+)?f?$", @")", @";" };
        static List<string> Values = new List<string>() { "Complex", @"^(?!\b(?:Complex|new)\b)\b[a-zA-Z_]+\b$", "=", "new", "Complex", "(", @"^-?\d+(\.\d+)?f?$", ",", @"^-?\d+(\.\d+)?f?$", ")", ";" };


        private static int tetraCounter = 1;
        private static List<string> tetraCodes;


        public static List<string> AnalyzeLine(string line)
        {
            RecursiveDescentParser parser = new RecursiveDescentParser(line);
            return parser.Parse(); 
        }

        private static string E(string expression)
        {
            Console.WriteLine($"E: {expression}"); // Debug
            string T_result = T(expression);
            string A_result = A(T_result);
            Console.WriteLine($"E result: {A_result}"); // Debug
            return A_result;
        }

        // 2. A → ε | + TA | - TA
        private static string A(string inheritedValue)
        {
            Console.WriteLine($"A: {inheritedValue}"); // Debug

            string[] terms = SplitIntoTerms(inheritedValue);

            if (terms.Length == 0)
            {
                Console.WriteLine("A -> ε"); // Debug
                return inheritedValue; // A → ε
            }

            if (terms[0] == "+" || terms[0] == "-")
            {
                string operation = terms[0];
                string TA_expression = string.Join("", terms.Skip(1));
                string T_result = T(TA_expression);
                string tempVariable = GenerateTemporaryVariable(tetraCodes.Count);
                tetraCodes.Add(FormatTetrad(tetraCounter++, operation + " ", inheritedValue, T_result, tempVariable));
                Console.WriteLine($"A result: {tempVariable}"); // Debug
                return tempVariable;
            }

            Console.WriteLine("A -> ε"); // Debug
            return inheritedValue; // A → ε
        }

        // 3. T → OB
        private static string T(string expression)
        {
            Console.WriteLine($"T: {expression}"); // Debug
            string O_result = O(expression);
            string B_result = B(O_result);
            Console.WriteLine($"T result: {B_result}"); // Debug
            return B_result;
        }

        // 4. B → ε | *OB | /OB
        private static string B(string inheritedValue)
        {
            Console.WriteLine($"B: {inheritedValue}"); // Debug
            string[] terms = SplitIntoTerms(inheritedValue);

            if (terms.Length == 0)
            {
                Console.WriteLine("B -> ε"); // Debug
                return inheritedValue; // B → ε
            }

            if (terms[0] == "*" || terms[0] == "/")
            {
                string operation = terms[0];
                string OB_expression = string.Join("", terms.Skip(1));
                string O_result = O(OB_expression);
                string tempVariable = GenerateTemporaryVariable(tetraCodes.Count);
                tetraCodes.Add(FormatTetrad(tetraCounter++, operation + " ", inheritedValue, O_result, tempVariable));
                Console.WriteLine($"B result: {tempVariable}"); // Debug
                return tempVariable;
            }

            Console.WriteLine("B -> ε"); // Debug
            return inheritedValue; // B → ε
        }

        // 5. O → id | (E)
        private static string O(string expression)
        {
            Console.WriteLine($"O: {expression}"); // Debug
            string[] terms = SplitIntoTerms(expression);

            if (terms.Length == 1)
            {
                string term = terms[0].Trim();
                if (term.StartsWith("(") && term.EndsWith(")"))
                {
                    string innerExpression = term.Substring(1, term.Length - 2);
                    string E_result = E(innerExpression);
                    Console.WriteLine($"O result (E): {E_result}"); // Debug
                    return E_result;
                }
                else
                {
                    Console.WriteLine($"O result (id): {term}"); // Debug
                    return term; // O → id
                }
            }
            else
            {
                Console.WriteLine("Ошибка: Некорректный операнд."); // Debug
                throw new Exception("Ошибка: Некорректный операнд.");
            }
        }

        private static string[] SplitIntoTerms(string expression)
        {
            List<string> terms = new List<string>();
            string currentTerm = "";
            bool lastWasOperator = true;

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (c == '+' || c == '-' || c == '*' || c == '/')
                {
                    if (currentTerm.Length > 0)
                    {
                        terms.Add(currentTerm.Trim());
                    }
                    terms.Add(c.ToString());
                    currentTerm = "";
                    lastWasOperator = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (currentTerm.Length > 0)
                    {
                        terms.Add(currentTerm.Trim());
                    }
                    currentTerm = "";
                    lastWasOperator = false;
                }
                else if (c == '(' || c == ')')  // Treat parentheses as separate terms
                {
                    if (currentTerm.Length > 0)
                    {
                        terms.Add(currentTerm.Trim());
                    }
                    terms.Add(c.ToString());
                    currentTerm = "";
                    lastWasOperator = false;
                }
                else if (char.IsLetterOrDigit(c)) // Handle identifiers
                {
                    currentTerm += c;
                }
                else
                {
                    if (currentTerm.Length > 0)
                    {
                        terms.Add(currentTerm.Trim());
                    }
                    currentTerm = "";
                }

            }

            if (currentTerm.Length > 0)
            {
                terms.Add(currentTerm.Trim());
            }

            return terms.ToArray();
        }

        private static string GenerateTemporaryVariable(int index)
        {
            return "t" + index;
        }

        private static string FormatTetrad(int number, string operation, string arg1, string arg2, string result)
        {
            return $"Тетрад {number} : {operation} {arg1} {arg2} {result}";
        }




        //private static List<string> AnalyzeLine(string line)
        //{
        //    List<string> errors = new List<string>();
        //    List<(string, int)> MegaErrors = new List<(string, int)>();
        //    var result = CleanStringRegex(line);
        //    if (result.Item1.Count > 0)
        //    {
        //        for (int k = 0; k < result.Item1.Count; k++)
        //        {
        //            MegaErrors.Add(("Обнаружен недопустимый токен: " + (result.Item1)[k] + " на позиции " + (result.Item2)[k] + "\n", (result.Item2)[k]));
        //            errors.Add("Обнаружен недопустимый токен: " + (result.Item1)[k] + " на позиции " + (result.Item2)[k] + "\n");
        //        }
        //    }
        //    List<string> Tokens = Tokenize(result.Item3);
        //    int Counter = 0;
        //    for (int l = 0; l < Tokens.Count; l++)
        //    {
        //        if (Counter == 0 || Counter == 3 || Counter == 4) //ключевые слова
        //        {
        //            if (Tokens[l] == Values[Counter])
        //            {
        //                Counter++;
        //            }
        //            else
        //            {
        //                if (Regex.IsMatch(Tokens[l], Types[Counter]))
        //                {
        //                    if (Counter == 0)
        //                    {
        //                        if (Regex.IsMatch(Tokens[l + 1], Types[Counter]))
        //                        {
        //                            MegaErrors.Add(("Ожидалось: " + Values[Counter] + " получено " + Tokens[l] + "\n", CountChars(Tokens, l)));
        //                            Counter++;
        //                        }
        //                        else
        //                        {
        //                            MegaErrors.Add(("Отсутствует обязательный токен: Complex\n", CountChars(Tokens, l)));
        //                            l--;
        //                            Counter++;
        //                        }
        //                    }
        //                    if (Counter == 3)
        //                    {
        //                        if (Regex.IsMatch(Tokens[l + 1], Types[l + 1]))
        //                        {
        //                            MegaErrors.Add(("Ожидалось: " + Values[Counter] + " получено " + Tokens[l] + "\n", CountChars(Tokens, l)));
        //                            Counter++;
        //                        }
        //                        else
        //                        {
        //                            if (Tokens[l] == "Complex")
        //                            {
        //                                MegaErrors.Add(("Отсутствует обязательный токен: new\n", CountChars(Tokens, l)));
        //                                l--;
        //                                Counter++;
        //                            }
        //                            else
        //                            {
        //                                MegaErrors.Add(("Ожидалось: " + Values[Counter] + " получено " + Tokens[l] + "\n", CountChars(Tokens, l)));//
        //                                Counter++;
        //                            }
        //                        }
        //                    }
        //                    if (Counter == 4)
        //                    {
        //                        if (Tokens[l + 1]!="Complex")
        //                        MegaErrors.Add(("Ожидалось: " + Values[Counter] + " получено " + Tokens[l+1] + "\n", CountChars(Tokens, l))); //
        //                        Counter++;
        //                    }
        //                }
        //                else
        //                {
        //                    int Start = l;
        //                    bool founded = false;
        //                    int Place = -1;
        //                    for (int smth = Start + 1; smth < Tokens.Count; smth++)
        //                    {
        //                        if (Tokens[l] == Values[Counter])
        //                        {
        //                            founded = true;
        //                            Place = smth;
        //                            break;
        //                        }
        //                    }
        //                    if (founded)
        //                    {
        //                        l = Place - 1;
        //                        for (int vvv = Start; vvv < Place; vvv++)
        //                        {
        //                            MegaErrors.Add(("Обнаружен неожиданный токен: " + Tokens[vvv] + "\n", CountChars(Tokens, vvv)));
        //                            //errors.Add("Обнаружен неожиданный токен: " + Tokens[vvv]);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (Counter == 3)
        //                        {
        //                            Counter++;
        //                            l--;
        //                            MegaErrors.Add(("Отсутствует обязательный токен: new\n", CountChars(Tokens, l)));
        //                            //errors.Add("Отсутствует обязательный токен: new");
        //                        }
        //                        else if (Counter == 4)
        //                        {
        //                            Counter++;
        //                            l--;
        //                            MegaErrors.Add(("Отсутствует обязательный токен: Complex\n", CountChars(Tokens, l)));
        //                            //errors.Add("Отсутствует обязательный токен: Complex");
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else if (Counter == 1 || Counter == 6 || Counter == 8) // наборы символов имени и чисел
        //        {
        //            if (Regex.IsMatch(Tokens[l], Types[Counter]) && Tokens[l] != "Complex" && Tokens[l] != "new")
        //            {
        //                Counter++;
        //            }
        //            else
        //            {
        //                int Start = l;
        //                bool founded = false;
        //                int Place = -1;
        //                for (int smth = Start + 1; smth < Tokens.Count; smth++)
        //                {
        //                    if (Regex.IsMatch(Tokens[smth], Types[Counter]) && Tokens[smth] != "Complex" && Tokens[smth] != "new")
        //                    {
        //                        founded = true;
        //                        Place = smth;
        //                        break;
        //                    }
        //                }
        //                if (founded)
        //                {
        //                    l = Place - 1;
        //                    for (int vvv = Start; vvv < Place; vvv++)
        //                    {
        //                        if (!Regex.IsMatch(Tokens[vvv], Types[0]))
        //                        MegaErrors.Add(("Обнаружен неожиданный токен: " + Tokens[vvv] + "\n", CountChars(Tokens, vvv)));
        //                        //errors.Add("Обнаружен неожиданный токен: " + Tokens[vvv]);
        //                    }
        //                }
        //                else
        //                {
        //                    if (Counter == 1)
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: имя переменной\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: имя переменной"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                    else
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: число\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: число"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                }
        //            }
        //        }
        //        else if (Counter == 2 || Counter == 5 || Counter == 7 || Counter == 9 || Counter == 10) //знаки
        //        {
        //            if (Tokens[l] == Values[Counter])
        //            {
        //                Counter++;
        //            }
        //            else
        //            {
        //                int Start = l;
        //                bool founded = false;
        //                int Place = -1;
        //                for (int smth = Start + 1; smth < Tokens.Count; smth++)
        //                {
        //                    if (Tokens[smth] == Values[Counter])
        //                    {
        //                        founded = true;
        //                        Place = smth;
        //                        break;
        //                    }
        //                }
        //                if (founded)
        //                {
        //                    l = Place - 1;
        //                    for (int vvv = Start; vvv < Place - 1; vvv++)
        //                    {
        //                        MegaErrors.Add(("Обнаружен неожиданный токен: " + Tokens[vvv] + "\n", CountChars(Tokens, vvv)));
        //                        //errors.Add("Обнаружен неожиданный токен: " + Tokens[vvv]);
        //                    }
        //                }
        //                else
        //                {
        //                    if (Counter == 2)
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: =\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: знак равно"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                    if (Counter == 5)
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: (\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: открывающая скобка"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                    if (Counter == 7)
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: ,\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: запятая"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                    if (Counter == 9)
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: )\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: закрывающая скобка"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                    if (Counter == 10)
        //                    {
        //                        MegaErrors.Add(("Отсутствует обязательный токен: ;\n", CountChars(Tokens, Start)));
        //                        //errors.Add(("Отсутствует обязательный токен: точка с запятой"));
        //                        Counter++;
        //                        l--;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    errors = MegaErrors.OrderBy(e => e.Item2).Select(e => e.Item1).ToList();
        //    if (Counter < 11)
        //    {
        //        for (int c = Counter; c < Types.Count; c++)
        //        {
        //            errors.Add("Отсутствует обязательный токен: " + Missing[c]+"\n");
        //        }
        //    }
        //    return errors;
        //}

        private static Dictionary<int, string> Missing = new Dictionary<int, string>() { {10,";" },{9, ")"},{8,"число"},{7,","}, {6,"число"}, {5,"("},{4,"Complex"}, {3, "new"}, {2,"="}, {1,"имя переменной"},{0,"Complex" } };

        public static int CountChars(List<string> Tokens, int index)
        {
            int ToReturn = 0;
            for (int i = 0; i < index; i++)
            {
                ToReturn += Tokens[i].Length;
            }
            return ToReturn;
        }
        public static List<string> Tokenize(string input)
        {
            List<string> tokens = new List<string>();
            string pattern = @"(\b[a-zA-Z]\w*\b)|(-?\d+\.?\d*f?)|([^\w\s])";  // Modified regex

            foreach (Match match in Regex.Matches(input, pattern))
            {
                tokens.Add(match.Value);
            }

            return tokens;
        }


        public static (List<string>, List<int>, string) CleanStringRegex(string input)
        {
            List<string> substrings = new List<string>();
            List<int> indices = new List<int>();
            StringBuilder cleanedString = new StringBuilder();

            int startIndex = 0;
            int currentIndex = 0;

            while (currentIndex < input.Length)
            {
                if (invalidChars.Contains(input[currentIndex]))
                {
                    if (currentIndex > startIndex)
                    {
                        cleanedString.Append(input.Substring(startIndex, currentIndex - startIndex));
                    }
                    int invalidStartIndex = currentIndex;
                    while (currentIndex < input.Length && invalidChars.Contains(input[currentIndex]))
                    {
                        currentIndex++;
                    }
                    substrings.Add(input.Substring(invalidStartIndex, currentIndex - invalidStartIndex));
                    indices.Add(invalidStartIndex);
                    startIndex = currentIndex;
                }
                else
                {
                    currentIndex++;
                }
            }
            if (startIndex < input.Length)
            {
                cleanedString.Append(input.Substring(startIndex));
            }

            return (substrings, indices, cleanedString.ToString());
        }


        public class RecursiveDescentParser
        {
            private readonly string _input;
            private int _currentIndex;
            private readonly List<string> _errors = new List<string>();

            public RecursiveDescentParser(string input)
            {
                _input = input;
                _currentIndex = 0;
            }

            public List<string> Parse()
            {
                WhileStatement();
                return _errors;
            }

            private void WhileStatement()
            {
                Expect("do", "do");
                Stmt();
                Expect("while", "while");
                Cond();
                Expect(";", ";");
            }

            private void Cond()
            {
                LogExpr();
                while (Match("or"))
                {
                    LogExpr();
                }
            }

            private void LogExpr()
            {
                RelExpr();
                while (Match("and"))
                {
                    RelExpr();
                }
            }

            private void RelExpr()
            {
                Operand();
                if (Match("rel"))
                {
                    Operand();
                }
            }

            private void Operand()
            {
                if (IsVar())
                {
                    Consume(); // Consume the var
                }
                else if (IsConst())
                {
                    Consume(); // Consume the const
                }
                else
                {
                    AddError("Expected variable or constant.", "operand");
                }
            }

            private void Stmt()
            {
                if (IsVar())
                {
                    Consume(); // Consume the var
                    Expect("as", "as", "statement"); // Changed error message to be more specific
                    ArithExpr();
                }
                else
                {
                    AddError("Expected variable in statement.", "statement");
                }
            }

            private void ArithExpr()
            {
                Operand();
                while (Match("ao"))
                {
                    Operand();
                }
            }

            private void Expect(string expectedToken, string context, string? customMessage = null)
            {
                string token = GetNextToken();

                if (token == null)
                {
                    AddError($"Expected '{expectedToken}', but found end of input.", context);
                    return;
                }

                if (token != expectedToken)
                {
                    string errorMessage = customMessage != null ? $"Expected '{expectedToken}', but found {token}" : $"Expected '{expectedToken}', but found '{token}'";
                    AddError(errorMessage, context);
                    //Try to recover by skipping the unexpected token
                    // Consume(); // This might cause issues if it skips the real expected token later
                }
                else
                {
                    Consume(); // consume the current token
                }
            }


            private bool Match(string expectedToken)
            {
                string token = GetNextToken();

                if (token == null)
                {
                    return false;
                }

                if (token == expectedToken)
                {
                    Consume();
                    return true;
                }

                return false;
            }

            private string GetNextToken()
            {
                // Skip whitespace
                while (_currentIndex < _input.Length && char.IsWhiteSpace(_input[_currentIndex]))
                {
                    _currentIndex++;
                }

                if (_currentIndex >= _input.Length)
                {
                    return null;
                }

                char currentChar = _input[_currentIndex];

                if (char.IsLetter(currentChar))
                {
                    // Variable or keyword
                    int start = _currentIndex;
                    while (_currentIndex < _input.Length && char.IsLetter(_input[_currentIndex]))
                    {
                        _currentIndex++;
                    }
                    return _input.Substring(start, _currentIndex - start);
                }
                else if (char.IsDigit(currentChar))
                {
                    // Constant
                    int start = _currentIndex;
                    while (_currentIndex < _input.Length && char.IsDigit(_input[_currentIndex]))
                    {
                        _currentIndex++;
                    }
                    return _input.Substring(start, _currentIndex - start);
                }
                else if (currentChar == '<' || currentChar == '>' || currentChar == '=' || currentChar == '!')
                {
                    //Possible relational operator
                    int start = _currentIndex;
                    _currentIndex++;
                    //Check for a second character (=) to make <=, >=, ==, !=
                    if (_currentIndex < _input.Length && _input[_currentIndex] == '=')
                    {
                        _currentIndex++;
                    }

                    return "rel";
                }
                else if (currentChar == '+' || currentChar == '-')
                {
                    _currentIndex++;
                    return "ao";
                }
                else if (currentChar == ';')
                {
                    _currentIndex++;
                    return ";";
                }
                else
                {
                    AddError($"Invalid character: '{currentChar}'.", "general");
                    _currentIndex++; // Skip the invalid character to continue parsing
                    return GetNextToken(); // try the next token
                }
            }

            private void Consume()
            {
                //Helper to move the index forward after successfully matching a token.
                _currentIndex++;
            }

            private void AddError(string errorMessage, string context)
            {
                //Format error messages
                string formattedMessage = "";
                string foundToken = "";
                if (errorMessage.Contains("but found"))
                {
                    int foundStartIndex = errorMessage.IndexOf("found ") + "found ".Length;
                    foundToken = errorMessage.Substring(foundStartIndex, errorMessage.Length - foundStartIndex - 1);
                }
                if (context == "as")
                {
                    formattedMessage = $"Ошибка ({_currentIndex}) - ожидалось {context}, получено выражение";
                }
                else if (context == "statement")
                {
                    formattedMessage = $"Ошибка ({_currentIndex}) - ожидалось {context}, получено {foundToken}";
                }
                else if (context == "while")
                {
                    formattedMessage = $"Ошибка ({_currentIndex}) - ожидалось {context}, получено {foundToken}";
                }
                else if (context == "operand")
                {
                    formattedMessage = $"Ошибка ({_currentIndex}) - ожидалось {context}, получено {foundToken}";
                }
                else
                {
                    formattedMessage = $"Ошибка ({_currentIndex}) - {errorMessage}";
                }

                _errors.Add(formattedMessage);
            }

            private bool IsVar()
            {
                if (_currentIndex >= _input.Length) return false;
                string token = GetNextToken(); //Use peek instead of getNextToken

                // Reset currentIndex, the method must not affect the parser internal state
                _currentIndex -= token?.Length ?? 0;

                return !string.IsNullOrEmpty(token) && char.IsLetter(token[0]) && !IsKeyword(token);
            }


            private bool IsConst()
            {
                if (_currentIndex >= _input.Length) return false;
                string token = GetNextToken(); //Use peek instead of getNextToken

                // Reset currentIndex, the method must not affect the parser internal state
                _currentIndex -= token?.Length ?? 0;

                return !string.IsNullOrEmpty(token) && char.IsDigit(token[0]);
            }
            private bool IsKeyword(string token)
            {
                return token == "do" || token == "while" || token == "and" || token == "or";
            }

        }

    }
    }
