namespace ExcelFormulaAnalyzer
{
    class Program
    {
        static void Main()
        {
            var formulas = new List<string>
            {
                "A1/B1/C1",
                "SUMIFS(C2:C6,C2:C6,\">3\")^(C2+C3)^C3",
                "C10+C11*C12/(C13+C13/C14)+MAX(C10-C12,CHOOSE(C15,C10*C11,C16))",
                "(C22+C23/C24)^(CHOOSE(C25,C26,C27))"
            };
            var tokenService = new TokenService();
            Console.WriteLine($"Source formula: {formulas[2].Replace("/", "*1/")}\n");
            Console.WriteLine(string.Join("\n", tokenService.Tokenize(formulas[2].Replace("/", "*1/"))));
        }
    }
}