﻿using System.Runtime.CompilerServices;

namespace Alexvis;

public static class Score
{
    static readonly int[] MaterialValueTable = { 100, 300, 315, 500, 900 };

    static readonly int[][] PiecePositionValueTable =
    {
        new[]
        { // Pawns
            0,  0,  0,  0,  0,  0,  0,  0,
            15, 15, 15, 15, 15, 15, 15, 15,
            6,  6,  6,  4,  4,  6,  6,  6,
            3,  3,  3,  5,  5,  3,  3,  3,
            5,  5,  6,  12, 12, 6,  5,  5,
            5,  7,  5,  8,  8,  5,  7,  5,
            8,  10, 8, 0,  0,  8, 10, 8,
            0,  0,  0,  0,  0,  0,  0,  0,
        },
        new[]
        { // Knights
            -8, -5, -3, -3, -3, -3, -5, -8,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 8, 8, 8, 8, 0, -5,
            -3, 0, 8, 10, 10, 8, 0, -3, 
            -3, 0, 8, 10, 10, 8, 0, -3,
            -5, 0, 6, 6, 6, 6, 0, -5, 
            -8, 0, 0, 0, 0, 0, 0, -8,
            -10, -8, -5, -5, -5, -5, -8, -10,
        },
        new[]
        { // Bishops
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 5, 0, 0, 0, 0, 5, 0, 
            0, 5, 8, 10, 10, 8, 5, 0, 
            0, 5, 5, 8, 8, 5, 5, 0,
            0, 5, 0, 5, 5, 0, 5, 0, 
            0, 0, 0, 0, 0, 0, 0, 0,
        },
        new[]
        { // Rooks
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
            10, 0, 0, 0, 0, 0, 0, 10, 
            5, 7, 7, 7, 7, 7, 7, 5, 
            2, 0, 0, 0, 0, 0, 0, 2, 
            0, 2, 3, 4, 4, 3, 2, 0, 
        },
        new[]
        { // Queens
            0, 0, 0, 0, 0, 0, 0, 0, 
            4, 8, 8, 8, 8, 8, 8, 4, 
            3, 6, 6, 6, 6, 6, 6, 3, 
            0, 4, 4, 8, 8, 4, 4, 0, 
            0, 4, 4, 10, 10, 4, 4, 0, 
            0, 4, 4, 7, 7, 4, 4, 0, 
            0, 2, 2, 2, 2, 2, 2, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
        },
        new[]
        { // Kings
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 
            -10, -20, -20, -20, -20, -20, -20, -10, 
            -10, -20, -20, -20, -20, -20, -20, -10, 
            -10, -20, -20, -20, -20, -20, -20, -10, 
            -10, -20, -20, -20, -20, -20, -20, -10, 
            -5, -10, -20, -20, -20, -20, -10, -5, 
            0, 20, -5, -10, -10, -5, 20, 0, 
        },
    };

    static Score()
    {
        // Because the top left is the 0-index in the preset arrays, they need to be reversed for the white perspective.
        // They can remain intact for the black perspective.
        PiecePositionValueTable = PiecePositionValueTable.Select(t => t.Reverse().ToArray()).Concat(PiecePositionValueTable).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int PiecePositionValue(Side s, PieceType pt, int i) => PiecePositionValueTable[(s == Side.White ? 0 : 6) + (int)pt][i];
    
    public static int Static(Position pos, Span<Move> moves, int mslen)
    {
        int value = 0;
        ulong st;
        int us = (int)pos.Us();
        int idx;
        
        foreach (var side in Sides.NotBoth)
            foreach (var pt in PieceTypes.NotNone)
            {
                st = pos.State[(int)side][(int)pt];
                if (pt != PieceType.King) value += MaterialValueTable[(int)pt] * BB.PopCount(st) * (us == (int)side ? 1 : -1);

                while (st != 0)
                {
                    idx = BB.LSBIndex(st);
                    value += PiecePositionValue(side, pt, idx) * (us == (int)side ? 1 : -1);
                    st ^= BB.FromIndex(idx);
                }
            }

        if (mslen == -1) value += BB.PopCount(pos.Occupancy[us]) * 2; // Estimation in case we don't know the available moves.
        for (int i = 0; i < mslen; i++)
            if (moves[i].HasFlag(Move.Flag.Capture)) value++;

        return value + mslen >> 2;
    }

    public static int FromMatePly(int ply) => -1000000000 + ply;
    public static int ToMatePly(int score) => 1000000000 - Math.Abs(score);
    public static bool IsMate(int score) => Math.Abs(score) is > 999000000 and <= 1000000000;
}