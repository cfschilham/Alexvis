using System.Numerics;
using System.Runtime.CompilerServices;

namespace BughouseChess.Core;

public static class Bitboard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(ulong bb, int i) => (bb >> i & 1) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FromIndex(int i) => 1UL << i;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Index(int file, int rank) => file + 8 * rank;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Rank(int i) => i switch
    {
        >= 0 and < 8 => 0,
        >= 8 and < 16 => 1,
        >= 16 and < 24 => 2,
        >= 24 and < 32 => 3,
        >= 32 and < 40 => 4,
        >= 40 and < 48 => 5,
        >= 48 and < 56 => 6,
        >= 56 and < 64 => 7,
        _ => -1,
    };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int File(int i) => i % 8;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LSBIndex(ulong bb) => BitOperations.TrailingZeroCount(bb);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InBounds(int i) => i is >= 0 and < 64;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong bb) => BitOperations.PopCount(bb);
    
    public static string ToString(ulong bb)
    {
        string o = "";
        for (int row = 7; row >= 0; row--)
        {
            for (int col = 0; col < 8; col++) o += IsSet(bb, Index(col, row)) ? "1 " : ". ";
            o += "\n";
        }

        return o;
    }

    public static int UCIToIndex(string s)
    {
        if (s.Length != 2)
            throw new ArgumentException("Invalid UCI string. Should be exactly 2 characters.", nameof(s));

        int file = s[0] - 'a'; // File (column) is first character, 'a'..'h' -> 0..7.
        int rank = s[1] - '1'; // Rank (row) is second character, '1'..'8' -> 0..7.

        return Index(file, rank);
    }

    public static string IndexToUCI(int i)
    {
        if (!InBounds(i)) throw new ArgumentOutOfRangeException(nameof(i), "Index out of chess board boundaries.");

        int file = File(i);
        int rank = Rank(i);

        char fileChar = (char)('a' + file); 
        char rankChar = (char)('1' + rank);

        return $"{fileChar}{rankChar}";
    }

    public static string MoveToUCI(Move move)
    {
        var from = IndexToUCI(move.GetFrom());
        var to = IndexToUCI(move.GetTo());

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

    public static Move SelectMoveFromUCI(string s, List<Move> moves)
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
        
        int from = UCIToIndex(fromUCI);
        int to = UCIToIndex(toUCI);

        return moves.First(m => m.GetFrom() == from && m.GetTo() == to && m.GetPromotion() == promo);
    }

    public static readonly ulong Rank7 = 0xFF000000000000;
    public static readonly ulong Rank6 = 0xFF0000000000;
    public static readonly ulong Rank2 = 0xFF00;
    public static readonly ulong Rank3 = 0xFF0000;
    public static readonly ulong NotRank1 = 0xFFFFFFFFFFFFFF00;
    public static readonly ulong Rank1 = 0x00000000000000FF;
    public static readonly ulong NotRank8 = 0xFFFFFFFFFFFFFF;
    public static readonly ulong Rank8 = 0x00000000000000;
}
