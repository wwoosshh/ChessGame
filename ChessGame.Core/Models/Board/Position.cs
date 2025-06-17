namespace ChessGame.Core.Models.Board
{
    public class Position
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public Position(string notation) // e.g., "e4"
        {
            if (notation.Length != 2)
                throw new ArgumentException("Invalid notation");

            Column = notation[0] - 'a';
            Row = notation[1] - '1';
        }

        public bool IsValid()
        {
            return Row >= 0 && Row < 8 && Column >= 0 && Column < 8;
        }

        public string ToNotation()
        {
            return $"{(char)('a' + Column)}{Row + 1}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Position other)
                return Row == other.Row && Column == other.Column;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public static bool operator ==(Position left, Position right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }
    }
}