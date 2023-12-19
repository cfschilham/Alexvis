namespace Alexvis;

public static class UCI
{
    public static int ToIndex(string s)
    {
        if (s.Length != 2)
            throw new ArgumentException("Invalid UCI string. Should be exactly 2 characters.", nameof(s));

        int file = s[0] - 'a'; // File (column) is first character, 'a'..'h' -> 0..7.
        int rank = s[1] - '1'; // Rank (row) is second character, '1'..'8' -> 0..7.

        return BB.Index(file, rank);
    }

    public static string FromIndex(int i)
    {
        if (!BB.InBounds(i)) throw new ArgumentOutOfRangeException(nameof(i), "Index out of chess board boundaries.");

        int file = BB.File(i);
        int rank = BB.Rank(i);

        char fileChar = (char)('a' + file); 
        char rankChar = (char)('1' + rank);

        return $"{fileChar}{rankChar}";
    }

    public static string FromMove(Move move)
    {
        var from = FromIndex(move.GetFrom());
        var to = FromIndex(move.GetTo());

        var flags = move.GetFlags();
        if (!Move.HasFlag(flags, Move.Flag.Promotion)) return from + to;
        switch (move.GetPromotion())
        {
            case PieceType.Queen: return from + to + 'q';
            case PieceType.Rook: return from + to + 'r';
            case PieceType.Bishop: return from + to + 'b';
            case PieceType.Knight: return from + to + 'n';
            default:
                throw new ArgumentException($"Invalid promotion piece");
        }
        
    }

    public static Move SelectMove(string s, List<Move> moves)
    {
        PieceType promo = PieceType.None;
        if (s.Length == 5)
        {
            char promotionChar = s[4];
            switch (promotionChar)
            {
                case 'q': promo = PieceType.Queen; break;
                case 'r': promo = PieceType.Rook; break;
                case 'b': promo = PieceType.Bishop; break;
                case 'n': promo = PieceType.Knight; break;
                default:
                    throw new ArgumentException($"Invalid promotion piece: {promotionChar}", nameof(s));
            } 
        }
        
        string fromUCI = s.Substring(0, 2);
        string toUCI = s.Substring(2, 2);
        
        int from = ToIndex(fromUCI);
        int to = ToIndex(toUCI);

        return moves.First(m => m.GetFrom() == from && m.GetTo() == to && m.GetPromotion() == promo);
    }
}