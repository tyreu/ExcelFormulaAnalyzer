namespace ExcelFormulaAnalyzer
{
    public static class RangeParser
    {
        public static string[] GetCellsFromRange(string range)
        {
            var cells = new List<string>();

            // Split the range into start and end cells
            var parts = range.Split(':');
            if (parts.Length != 2) throw new ArgumentException("Invalid range format");

            string startCell = parts[0];
            string endCell = parts[1];

            // Extract column letters and row numbers
            string startColumn = ExtractColumn(startCell);
            int startRow = ExtractRow(startCell);

            string endColumn = ExtractColumn(endCell);
            int endRow = ExtractRow(endCell);

            // Convert column letters to numbers for easier iteration
            int startColumnNumber = ConvertColumnToNumber(startColumn);
            int endColumnNumber = ConvertColumnToNumber(endColumn);

            // Generate all cells in the range
            for (int col = startColumnNumber; col <= endColumnNumber; col++)
            {
                for (int row = startRow; row <= endRow; row++)
                {
                    cells.Add($"{ConvertNumberToColumn(col)}{row}");
                }
            }

            return cells.ToArray();
        }

        private static string ExtractColumn(string cell)
        {
            int index = 0;
            while (index < cell.Length && char.IsLetter(cell[index]))
            {
                index++;
            }
            return cell.Substring(0, index);
        }

        private static int ExtractRow(string cell)
        {
            int index = 0;
            while (index < cell.Length && char.IsLetter(cell[index]))
            {
                index++;
            }
            return int.Parse(cell.Substring(index));
        }

        private static int ConvertColumnToNumber(string column)
        {
            int sum = 0;
            foreach (char c in column)
            {
                sum *= 26;
                sum += (c - 'A' + 1);
            }
            return sum;
        }

        private static string ConvertNumberToColumn(int columnNumber)
        {
            string column = string.Empty;
            while (columnNumber > 0)
            {
                columnNumber--;
                column = (char)('A' + (columnNumber % 26)) + column;
                columnNumber /= 26;
            }
            return column;
        }
    }
}