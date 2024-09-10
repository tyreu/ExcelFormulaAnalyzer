namespace Analyzer
{
    public class Token
    {
        public string Value { get; set; }
        public TokenType Type { get; set; }
        public int Level { get; set; }
        public int AdjustedValue { get; set; } = 0;

        public override string ToString() => $"{new string(' ', Level * 4)}{Value} - {Type}";
    }
}