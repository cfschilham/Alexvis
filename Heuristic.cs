using System.Runtime.CompilerServices;

namespace ArexMotor;

public static class Heuristic
{
    static readonly int[] MaterialValueTable = { 100, 300, 315, 500, 900 };

    static readonly int[][] PiecePositionValueTable =
    {
        new[]
        { // Pawns
            0,  0,  0,  0,  0,  0,  0,  0, // 8
            15, 15, 15, 15, 15, 15, 15, 15, // 7
            0,  0,  0,  0,  0,  0,  0,  0, // 6
            0,  0,  0,  5,  5,  0,  0,  0, // 5
            0,  0,  5,  10, 10, 5,  0,  0, // 4
            5,  7,  5,  8,  8,  5,  7,  5, // 3
            5,  10, 10, 0,  0,  10, 10, 5, // 2
            0,  0,  0,  0,  0,  0,  0,  0, // 1
        //  A   B   C   D   E   F   G   H
        },
        new[]
        { // Knights
            -8, -5, -3, -3, -3, -3, -5, -8, // 8
            -5, 0, 0, 0, 0, 0, 0, -5, // 7
            -5, 0, 8, 8, 8, 8, 0, -5, // 6
            -3, 0, 8, 10, 10, 8, 0, -3, // 5
            -3, 0, 8, 10, 10, 8, 0, -3, // 4
            -5, 0, 8, 8, 8, 8, 0, -5, // 3
            -8, 0, 0, 0, 0, 0, 0, -8, // 2
            -10, -8, -5, -5, -5, -5, -8, -10, // 1
        //  A   B   C   D   E   F   G   H
        },
        new[]
        { // Bishops
            0, 0, 0, 0, 0, 0, 0, 0, // 8
            0, 0, 0, 0, 0, 0, 0, 0, // 7
            0, 0, 0, 0, 0, 0, 0, 0, // 6
            0, 5, 0, 0, 0, 0, 5, 0, // 5
            0, 5, 8, 10, 10, 8, 5, 0, // 4
            0, 5, 0, 8, 8, 0, 5, 0, // 3
            0, 5, 0, 5, 5, 0, 5, 0, // 2
            0, 0, 0, 0, 0, 0, 0, 0, // 1
        //  A   B   C   D   E   F   G   H
        },
        new[]
        { // Rooks
            0, 0, 0, 0, 0, 0, 0, 0, // 8
            0, 0, 0, 0, 0, 0, 0, 0, // 7
            0, 0, 0, 0, 0, 0, 0, 0, // 6
            0, 0, 0, 0, 0, 0, 0, 0, // 5
            10, 0, 0, 0, 0, 0, 0, 10, // 4
            5, 7, 7, 7, 7, 7, 7, 5, // 3
            2, 0, 0, 0, 0, 0, 0, 2, // 2
            0, 2, 3, 4, 4, 3, 2, 0, // 1
        //  A   B   C   D   E   F   G   H
        },
        new[]
        { // Queens
            0, 0, 0, 0, 0, 0, 0, 0, // 8
            4, 8, 8, 8, 8, 8, 8, 4, // 7
            3, 6, 6, 6, 6, 6, 6, 3, // 6
            0, 4, 4, 8, 8, 4, 4, 0, // 5
            0, 4, 4, 10, 10, 4, 4, 0, // 4
            0, 4, 4, 7, 7, 4, 4, 0, // 3
            0, 2, 2, 2, 2, 2, 2, 0, // 2
            0, 0, 0, 0, 0, 0, 0, 0, // 1
        //  A   B   C   D   E   F   G   H
        },
        new[]
        { // Kings
            0, 0, 0, 0, 0, 0, 0, 0, // 8
            0, 0, 0, 0, 0, 0, 0, 0, // 7
            -10, -20, -20, -20, -20, -20, -20, -10, // 6
            -10, -20, -20, -20, -20, -20, -20, -10, // 5
            -10, -20, -20, -20, -20, -20, -20, -10, // 4
            -10, -20, -20, -20, -20, -20, -20, -10, // 3
            -5, -10, -20, -20, -20, -20, -10, -5, // 2
            0, 20, -5, -10, -10, -5, 20, 0, // 1
        //  A   B   C   D   E   F   G   H
        },
    };

    static Heuristic()
    {
        // Because the top left is the 0-index in the preset arrays, they need to be reversed for the white perspective.
        // They can remain intact for the black perspective.
        PiecePositionValueTable = PiecePositionValueTable.Select(t => t.Reverse().ToArray()).Concat(PiecePositionValueTable).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int PiecePositionValue(Side s, PieceType pt, int i) => PiecePositionValueTable[(s == Side.White ? 0 : 6) + (int)pt][i];
    
    public static int Eval(Position pos, int numMoves)
    {
        if (numMoves == 0)
        {
            if (MoveGenerator.IsChecked(pos))
                return int.MinValue;
            return 0;
        }
        
        int value = 0;
        ulong st;
        int idx;
        
        foreach (var side in Sides.NotBoth)
            foreach (var pt in PieceTypes.NotNone)
            {
                st = pos.State[(int)side][(int)pt];
                if (pt != PieceType.King) value += MaterialValueTable[(int)pt] * BB.PopCount(st) * (pos.Us() == side ? 1 : -1);

                while (st != 0)
                {
                    idx = BB.LSBIndex(st);
                    value += PiecePositionValue(side, pt, idx) * (pos.Us() == side ? 1 : -1);
                    st ^= BB.FromIndex(idx);
                }
            }

        return value;
    }
}