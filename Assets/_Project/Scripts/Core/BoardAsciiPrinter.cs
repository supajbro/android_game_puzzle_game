using System.Text;

namespace JellyRiot.Core
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Development diagnostic kept out of non-development player builds.</summary>
    public static class BoardAsciiPrinter
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void WriteTo(System.Action<string> output, BoardModel board)
        {
            output?.Invoke(Format(board));
        }

        public static string Format(BoardModel board)
        {
            var text = new StringBuilder();
            text.AppendLine("Jelly Riot board 7x9 (row 0 top)");
            text.AppendLine("    0 1 2 3 4 5 6");
            text.AppendLine("   +-------------+");
            for (int row = 0; row < BoardModel.Height; row++)
            {
                text.Append(row).Append("  |");
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    text.Append(ToCharacter(board.GetCell(column, row)));
                    if (column < BoardModel.Width - 1)
                    {
                        text.Append(' ');
                    }
                }

                text.AppendLine("|");
            }

            text.Append("   +-------------+");
            return text.ToString();
        }

        private static char ToCharacter(JellyColour colour)
        {
            switch (colour)
            {
                case JellyColour.Coral: return 'C';
                case JellyColour.Lemon: return 'L';
                case JellyColour.Aqua: return 'A';
                case JellyColour.Lilac: return 'I';
                case JellyColour.Mint: return 'M';
                default: return '.';
            }
        }
    }
#endif
}
