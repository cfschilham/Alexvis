using System.Numerics;
using System.Runtime.CompilerServices;

namespace Alexvis;

public static class BB
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

    public static readonly ulong Rank7 = 0xFF000000000000;
    public static readonly ulong Rank6 = 0xFF0000000000;
    public static readonly ulong Rank2 = 0xFF00;
    public static readonly ulong Rank3 = 0xFF0000;
    public static readonly ulong NotRank1 = 0xFFFFFFFFFFFFFF00;
    public static readonly ulong Rank1 = 0x00000000000000FF;
    public static readonly ulong NotRank8 = 0xFFFFFFFFFFFFFF;
    public static readonly ulong Rank8 = 0x00000000000000;
}
