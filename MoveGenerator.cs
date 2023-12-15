﻿using System.Xml;

namespace BughouseChess.Core;

public static class MoveGenerator
{
    static readonly int[] BishopDirections = { -9, -7, 7, 9 };
    static readonly int[] RookDirections = { -8, -1, 1, 8 };
    static readonly int[] KnightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 };
    static readonly int[] KingOffsets = { -9, -8, -7, -1, 1, 7, 8, 9 };

    static readonly PieceType[] PromotionTypes =
        { PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };

    struct Castle
    {
        public Side Side;
        public Position.Flag Flag;
        public Bitboard EmptyMask;
        public int[] NotChecked;
        public int KingFrom;
        public int KingTo;
    }

    static readonly Castle[] Castles =
    {
        new()
        {
            Side = Side.White,
            Flag = Position.Flag.CastleRightsWQ,
            EmptyMask = 0xE,
            NotChecked = new[] { 0, 1, 2, 3, 4 },
            KingFrom = 4,
            KingTo = 2,
        },
        new () {
            Side = Side.White,
            Flag = Position.Flag.CastleRightsWK,
            EmptyMask = 0x60,
            NotChecked = new [] {4, 5, 6, 7},
            KingFrom = 4,
            KingTo = 6,
        },
        new () {
            Side = Side.Black,
            Flag = Position.Flag.CastleRightsBQ,
            EmptyMask = 0xE00000000000000,
            NotChecked = new [] {56, 57, 58, 59, 60},
            KingFrom = 60,
            KingTo = 58,
        },
        new () {
            Side = Side.Black,
            Flag = Position.Flag.CastleRightsBK,
            EmptyMask = 0x6000000000000000,
            NotChecked = new [] {60, 61, 62, 63},
            KingFrom = 60,
            KingTo = 62,
        },
    };
    
    public static (int, int) RookCastleMove(int kingTo) => kingTo switch
    {
        2 => (0, 3),
        6 => (7, 5),
        58 => (56, 59),
        62 => (63, 61),
        _ => throw new ArgumentOutOfRangeException(nameof(kingTo), kingTo, null)
    };
    public static Position.Flag RookCastleFlag(int rookFrom) => rookFrom switch
    {
        0 => Position.Flag.CastleRightsWQ,
        7 => Position.Flag.CastleRightsWK,
        56 => Position.Flag.CastleRightsBQ,
        63 => Position.Flag.CastleRightsBK,
        _ => 0,
    };
    
    static Bitboard[][] BishopTable;
    static Bitboard[][] RookTable;
    
    static readonly Bitboard[] KnightMoves = new Bitboard[64];
    static readonly Bitboard[] KingMoves = new Bitboard[64];
    static readonly Bitboard[][] PawnAttacks = new Bitboard[64][];
    
    // public static void GenerateMagicBishopNumber()
    // {
    //     Bitboard[] moveMasks = new Bitboard[64];
    //     Bitboard[][] blockerMasks = new Bitboard[64][];
    //     
    //     for (int i = 0; i < 64; i++)
    //     {
    //         moveMasks[i] = Walk(i, RookDirections, false);
    //         blockerMasks[i] = GenerateBlockerMasks(moveMasks[i]);
    //     }
    //
    //     Random rand = new Random();
    //     ulong[] magics = new ulong[64];
    //     Bitboard[] exampleTable = new Bitboard[131072];
    //     for (int i = 0; i < 64; i++)
    //     {
    //         int bits = 15;
    //         
    //         WHILELOOP:
    //         while (true)
    //         {
    //             ulong magic = Magics.Rook15[i];
    //             foreach (var blockerMask in blockerMasks[i])
    //             {
    //                 exampleTable[blockerMask * magic >> (64 - bits)] = Walk(i, RookDirections, false, blockerMask);
    //             }
    //             
    //             foreach (var blockerMask in blockerMasks[i])
    //             {
    //                 if (exampleTable[blockerMask * magic >> (64 - bits)] !=
    //                     Walk(i, RookDirections, false, blockerMask)) goto WHILELOOP;
    //             }
    //             Console.WriteLine($"found {bits}-bit magic for index {i}: {magic:X}");
    //             magics[i] = magic;
    //             break;
    //         }
    //     }
    //
    //     for (int i = 0; i < 64; i++)
    //     {
    //         Console.WriteLine($"0x{magics[i]:X},");
    //     }
    // }

    public static void Init()
    {
        BishopTable = GenerateTable(BishopDirections, Magics.Bishop14, 14);
        RookTable = GenerateTable(RookDirections, Magics.Rook14, 14);
        for (int i = 0; i < 64; i++)
        {
            KingMoves[i] = Walk(i, KingOffsets, true);
            KnightMoves[i] = Walk(i, KnightOffsets, true);
        }

        foreach (var s in Sides.All)
        {
            PawnAttacks[(int)s] = new Bitboard[64];
            for (int from = 0; from < 64; from++)
            {
                foreach (var offset in new [] {-1, 1})
                {
                    int to = from + Position.PawnPush(s) + offset;
                    if (!Bitboard.InBounds(to) || Bitboard.Rank(to) != Bitboard.Rank(to - offset))
                        continue;
                    PawnAttacks[(int)s][from] |= Bitboard.FromIndex(to);
                }
            }
        }
    }

    static Bitboard[][] GenerateTable(int[] directions, ulong[] magics, int bits)
    {
        Bitboard[][] table = new Bitboard[64][];
        for (int i = 0; i < 64; i++)
        {
            table[i] = new Bitboard[1 << bits];
            Bitboard moveMask = Walk(i, directions, false);
            foreach (var blockerMask in GenerateBlockerMasks(moveMask))
                table[i][blockerMask * magics[i] >> (64-bits)] = Walk(i, directions, false, blockerMask);            
        }

        return table;
    }
    
    static Bitboard[] GenerateBlockerMasks(Bitboard moveMask)
    {
        int n = (int)Math.Pow(2, moveMask.PopCount());
        Bitboard[] blockerMasks = new Bitboard[n];
        for (int i = 0; i < n; i++)
        {
            // Copy the movement mask and index because we are going to change them, but we need the originals for the
            // next iteration.
            Bitboard mm2 = moveMask;
            int i2 = i;
            while (mm2 != 0 && i2 != 0)
            {
                // Shift the bits of i into the bits of the movement mask, bit by bit.
                blockerMasks[i] |= (ulong)i2 << mm2.LSBIndex() & Bitboard.FromIndex(mm2.LSBIndex());
                i2 >>= 1; // Use the next bit of i for the next iteration.
                mm2 ^= Bitboard.FromIndex(mm2.LSBIndex()); // Move on to the next bit index.
            }
        }

        return blockerMasks;
    }

    static Bitboard Walk(int from, int[] directions, bool singleStep, Bitboard blockerMask = default)
    {
        Bitboard result = 0;
        int to, rankDiff, fileDiff;
        foreach (var direction in directions)
        {
            to = from + direction;

            while (Bitboard.InBounds(to))
            {
                // Several checks to prevent edge flipping (i.e. going past the one edge of the board, such as the left,
                // and continuing on to end up on the other side with possibly a different rank). This is possible for
                // horizontal, bishop and knight movements. Vertical movements will be out of bounds.
                if (direction is -1 or 1 && Bitboard.Rank(to) != Bitboard.Rank(from)) break;
                rankDiff = Math.Abs(Bitboard.Rank(to) - Bitboard.Rank(from));
                fileDiff = Math.Abs(Bitboard.File(to) - Bitboard.File(from));
                if (BishopDirections.Contains(direction) && rankDiff != fileDiff) break;
                if (KnightOffsets.Contains(direction) && !(rankDiff == 1 && fileDiff == 2) && 
                    !(rankDiff == 2 && fileDiff == 1)) break;
                
                result |= Bitboard.FromIndex(to);
                if (blockerMask.IsSet(to)) break;
                if (singleStep) break;
                to += direction;
            }
        }

        return result;
    }

    public static void GeneratePawnAttacks(Position pos, List<Move> moves)
    {
        Bitboard pawns = pos.State[(int)pos.Us()][(int)PieceType.Pawn];
        
        int from, to;
        Bitboard atks;
        while (pawns != 0)
        {
            from = pawns.LSBIndex();
            atks = PawnAttacks[(int)pos.Us()][from] & pos.Occupancy[(int)pos.Opp()];
            atks &= pos.Us() == Side.White ? Bitboard.NotRank8 : Bitboard.NotRank1;
            while (atks != 0)
            {
                to = atks.LSBIndex();
                atks ^= Bitboard.FromIndex(to);
                moves.Add(new Move(from, to, PieceType.Pawn, Move.Flag.Capture, PieceType.None));
            }

            pawns ^= Bitboard.FromIndex(from);
        }
    }
    
    public static void GeneratePawnMoves(Position pos, List<Move> moves)
    {
        Bitboard oneForward = pos.PawnPush(pos.State[(int)pos.Us()][(int)PieceType.Pawn]) & ~pos.Occupancy[(int)Side.Both];
        Bitboard twoForward = pos.PawnPush(oneForward & (pos.Us() == Side.White ? Bitboard.Rank3 : Bitboard.Rank6));
        oneForward &= pos.Us() == Side.White ? Bitboard.NotRank8 : Bitboard.NotRank1;

        int from, to;
        while (oneForward != 0)
        {
            to = oneForward.LSBIndex();
            from = to - pos.PawnPush();
            moves.Add(new Move(from, to, PieceType.Pawn, Move.Flag.None, PieceType.None));
            if (twoForward.IsSet(to + pos.PawnPush()))
                moves.Add(new Move(from, to + pos.PawnPush(), PieceType.Pawn, Move.Flag.None, PieceType.None));
            oneForward ^= Bitboard.FromIndex(to);
        }
    }

    public static void GeneratePromotions(Position pos, List<Move> moves)
    {
        Bitboard pawns = pos.State[(int)pos.Us()][(int)PieceType.Pawn];
        pawns &= pos.Us() == Side.White ? Bitboard.Rank7 : Bitboard.Rank2;
        Bitboard oneForward = pos.PawnPush(pawns) & ~pos.Occupancy[(int)Side.Both];

        int from, to;
        Bitboard atks;
        while (pawns != 0)
        {
            from = pawns.LSBIndex();
            atks = PawnAttacks[(int)pos.Us()][from] & pos.Occupancy[(int)pos.Opp()];
            while (atks != 0)
            {
                to = atks.LSBIndex();
                foreach (var pt in PromotionTypes)
                    moves.Add(new Move(from, to, PieceType.Pawn, Move.Flag.Capture|Move.Flag.Promotion, pt));
                atks ^= Bitboard.FromIndex(to);
            }

            to = from + pos.PawnPush();
            if (oneForward.IsSet(to))
                foreach (var pt in PromotionTypes)
                    moves.Add(new Move(from, to, PieceType.Pawn, Move.Flag.Promotion, pt));
            pawns ^= Bitboard.FromIndex(from);
        }
    }

    public static void GenerateEnPassant(Position pos, List<Move> moves)
    {
        if (pos.EnPassantCapturable() == null) return;
        int capturable = (int)pos.EnPassantCapturable()!;
        Bitboard pawns = pos.State[(int)pos.Us()][(int)PieceType.Pawn];
        int from;
        foreach (var offset in new []{-1, 1})
        {
            from = capturable + offset;
            if (pawns.IsSet(from) || Bitboard.Rank(capturable) != Bitboard.Rank(from)) continue;
            moves.Add(new Move(from, capturable + pos.PawnPush(), PieceType.Pawn, Move.Flag.EnPassant, PieceType.None));
        }
    }

    public static void GenerateCastles(Position pos, List<Move> moves)
    {
        if (!pos.HasFlag(pos.Us() == Side.White ? Position.Flag.CastleRightsW : Position.Flag.CastleRightsB)) return;
        foreach (var c in Castles)
        {
            if (c.Side != pos.Us()) continue;
            if (!pos.HasFlag(c.Flag)) continue;
            if ((c.EmptyMask & pos.Occupancy[(int)Side.Both]) != 0) continue;
            foreach (var i in c.NotChecked)
                if (IsChecked(pos, pos.Us(), i))
                    goto next;
            moves.Add(new Move(c.KingFrom, c.KingTo, PieceType.King, Move.Flag.Castle, PieceType.None));
            next: ;
        }
        
    }

    public static void GeneratePieceMoves(Position pos, List<Move> moves, PieceType pt, bool capturesOnly = false)
    {
        Bitboard pieces = pos.State[(int)pos.Us()][(int)pt];
        int from, to;
        Bitboard moveMask, captureMask, occupancy;
        while (pieces != 0)
        {
            from = pieces.LSBIndex();
            switch (pt)
            {
                case PieceType.Knight:
                    moveMask = KnightMoves[from];
                    break;
                case PieceType.Bishop:
                    occupancy = pos.Occupancy[(int)Side.Both] & BishopTable[from][0];
                    moveMask = BishopTable[from][occupancy * Magics.Bishop14[from] >> 50];
                    break;
                case PieceType.Rook:
                    occupancy = pos.Occupancy[(int)Side.Both] & RookTable[from][0];
                    moveMask = RookTable[from][occupancy * Magics.Rook14[from] >> 50];
                    break;
                case PieceType.Queen:
                    occupancy = pos.Occupancy[(int)Side.Both] & BishopTable[from][0];
                    moveMask = BishopTable[from][occupancy * Magics.Bishop14[from] >> 50];
                    occupancy = pos.Occupancy[(int)Side.Both] & RookTable[from][0];
                    moveMask |= RookTable[from][occupancy * Magics.Rook14[from] >> 50];
                    break;
                case PieceType.King:
                    moveMask = KingMoves[from];
                    break;
                default: throw new ArgumentException();
            }
            
            captureMask = moveMask & pos.Occupancy[(int)pos.Opp()];
            moveMask &= ~pos.Occupancy[(int)Side.Both];

            while (captureMask != 0)
            {
                to = captureMask.LSBIndex();
                moves.Add(new Move(from, to, pt, Move.Flag.Capture, PieceType.None));
                captureMask ^= Bitboard.FromIndex(to);
            }

            if (capturesOnly)
            {
                pieces ^= Bitboard.FromIndex(from);
                continue;
            }
            
            while (moveMask != 0)
            {
                to = moveMask.LSBIndex();
                moves.Add(new Move(from, to, pt, 0, PieceType.None));
                moveMask ^= Bitboard.FromIndex(to);
            }
            pieces ^= Bitboard.FromIndex(from);
        }
    }

    public static void GenerateAllMoves(Position pos, List<Move> moves)
    {
        GeneratePawnMoves(pos, moves);
        GeneratePawnAttacks(pos, moves);
        GeneratePromotions(pos, moves);
        GenerateCastles(pos, moves);
        GenerateEnPassant(pos, moves);
        GeneratePieceMoves(pos, moves, PieceType.Knight);
        GeneratePieceMoves(pos, moves, PieceType.Bishop);
        GeneratePieceMoves(pos, moves, PieceType.Rook);
        GeneratePieceMoves(pos, moves, PieceType.Queen);
        GeneratePieceMoves(pos, moves, PieceType.King);
    }

    public static void GenerateCapturesAndPromotions(Position pos, List<Move> moves)
    {
        GeneratePawnAttacks(pos, moves);
        GeneratePromotions(pos, moves);
        GenerateEnPassant(pos, moves);
        GeneratePieceMoves(pos, moves, PieceType.Knight, true);
        GeneratePieceMoves(pos, moves, PieceType.Bishop, true);
        GeneratePieceMoves(pos, moves, PieceType.Rook, true);
        GeneratePieceMoves(pos, moves, PieceType.Queen, true);
        GeneratePieceMoves(pos, moves, PieceType.King, true);
    }

    public static bool IsChecked(Position pos) =>
        IsChecked(pos, pos.Us(), pos.State[(int)pos.Us()][(int)PieceType.King].LSBIndex());
    public static bool IsChecked(Position pos, Side s, int from)
    {
        Bitboard moveMask, occupancy;
        
        // Bishop and queen checks.
        occupancy = pos.Occupancy[(int)Side.Both] & BishopTable[from][0];
        moveMask = BishopTable[from][occupancy * Magics.Bishop14[from] >> 50];
        moveMask &= pos.State[(int)Position.OppOf(s)][(int)PieceType.Bishop] | pos.State[(int)Position.OppOf(s)][(int)PieceType.Queen];
        if (moveMask != 0) return true;
        
        // Rook and queen checks.
        occupancy = pos.Occupancy[(int)Side.Both] & RookTable[from][0];
        moveMask = RookTable[from][occupancy * Magics.Rook14[from] >> 50];
        moveMask &= pos.State[(int)Position.OppOf(s)][(int)PieceType.Rook] | pos.State[(int)Position.OppOf(s)][(int)PieceType.Queen];
        if (moveMask != 0) return true;
        
        // Knight checks.
        moveMask = KnightMoves[from] & pos.State[(int)Position.OppOf(s)][(int)PieceType.Knight];
        if (moveMask != 0) return true;
        
        // Pawn checks.
        moveMask = PawnAttacks[(int)s][from] & pos.State[(int)Position.OppOf(s)][(int)PieceType.Pawn];
        if (moveMask != 0) return true;
        
        // King checks (used for move legality check)
        moveMask = KingMoves[from] & pos.State[(int)Position.OppOf(s)][(int)PieceType.King];
        return moveMask != 0;
    }

    public static bool IsLegal(Position pos) => 
        !IsChecked(pos, pos.Opp(), pos.State[(int)pos.Opp()][(int)PieceType.King].LSBIndex());
}