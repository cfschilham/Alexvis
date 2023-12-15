using System.Numerics;

namespace BughouseChess.Core;

public struct Bitboard(ulong v)
{
    readonly ulong _v = v;
    public static implicit operator Bitboard(ulong v) => new(v);
    public static implicit operator ulong(Bitboard bb) => bb._v;
    public static ulong operator <<(Bitboard b, int n) => b._v << n;
    public static ulong operator >>(Bitboard b, int n) => b._v >> n;
    public static ulong operator ^(Bitboard b, ulong n) => b._v ^ n;
    public static ulong operator ^(Bitboard b, Bitboard n) => b._v ^ n._v;
    public static ulong operator &(Bitboard b, ulong n) => b._v & n;
    public static ulong operator &(Bitboard b, Bitboard n) => b._v & n._v;
    public static ulong operator |(Bitboard b, ulong n) => b._v | n;
    public static ulong operator |(Bitboard b, Bitboard n) => b._v | n._v;
    public static ulong operator ~(Bitboard b) => ~b._v;
    public static bool operator ==(Bitboard b, ulong n) => b._v == n;
    public static bool operator !=(Bitboard b, ulong n) => b._v != n;
    public bool IsSet(int i) => (v >> i & 1) != 0;
    public static Bitboard FromIndex(int i) => new(1UL << i);
    public static int Index(int file, int rank) => file + 8 * rank;
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

    public static int File(int i) => i % 8;
    public int LSBIndex() => BitOperations.TrailingZeroCount(_v);
    public static bool InBounds(int i) => i is >= 0 and < 64;
    public int PopCount() => BitOperations.PopCount(_v);
    public override string ToString()
    {
        string o = "";
        for (int row = 7; row >= 0; row--)
        {
            for (int col = 0; col < 8; col++)
            {
                o += IsSet(Index(col, row)) ? "1 " : ". ";
            }

            o += "\n";
        }

        return o;
    }

    public static readonly Bitboard Rank7 = 0xFF000000000000;
    public static readonly Bitboard Rank6 = 0xFF0000000000;
    public static readonly Bitboard Rank2 = 0xFF00;
    public static readonly Bitboard Rank3 = 0xFF0000;
    public static readonly Bitboard NotRank1 = 0xFFFFFFFFFFFFFF00;
    public static readonly Bitboard Rank1 = 0x00000000000000FF;
    public static readonly Bitboard NotRank8 = 0xFFFFFFFFFFFFFF;
    public static readonly Bitboard Rank8 = 0x00000000000000;
}
