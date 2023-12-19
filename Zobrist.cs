using System.Runtime.CompilerServices;

namespace Alexvis;

public static class Zobrist
{
    static ulong[] _table;

    static Zobrist()
    {
        // Create 2^5 (32) entries for each possible flag state of the position, 8 for each en passant file, 64 * 2 * 6
        // for each piece per side per square.
        _table = new ulong[32 + 8 + 64 * 6 * 2];
        Random rnd = new (0xA54B1C3);
        byte[] buffer = new byte[8];
        for (int i = 0; i < _table.Length; i++)
        {
            rnd.NextBytes(buffer);
            _table[i] = BitConverter.ToUInt64(buffer, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong PieceHash(int side, int pt, int idx) => _table[40 + pt * 64 + idx * (side + 1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FlagHash(Position.Flag f) => _table[(int)f];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EPCHash(int epc) => _table[32 + BB.File(epc)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FullHash(Position pos)
    {
        ulong st;
        int epc = pos.EnPassantCapturable();
        ulong hash = FlagHash(pos.Flags) ^ (epc != -1 ? EPCHash(epc) : 0);
        int idx;
        foreach (var s in Sides.NotBoth)
            foreach (var pt in PieceTypes.NotNone)
            {
                st = pos.State[(int)s][(int)pt];
                while (st != 0)
                {
                    idx = BB.LSBIndex(st);
                    hash ^= PieceHash((int)s, (int)pt, idx);
                    st ^= BB.FromIndex(idx);
                }
            }
        
        return hash;
    }
}