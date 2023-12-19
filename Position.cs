using System.Runtime.CompilerServices;

namespace ArexMotor;

// Represents a single chess position. A new one is created with each move. The index works as follows:
// 
// 8  56 57 58 59 60 61 62 63
// 7  48 49 50 51 52 53 54 55
// 6  40 41 42 43 44 45 46 47
// 5  32 33 34 35 36 37 38 39
// 4  24 25 26 27 28 29 30 31
// 3  16 17 18 19 20 21 22 23
// 2  08 09 10 11 12 13 14 15
// 1  00 01 02 03 04 05 06 07
//    A  B  C  D  E  F  G  H
//
// So index 0 is equal to A1, etc. Note, however, that index 0 also represents the most significant bit of the ulong. So
// _state[0] << 63 would left shift away all bits except the one representing H8 or index 63 (of white pawns in this
// case). 
//
// TODO
// 50 move rule
// 3 fold repetition
// fix checkmate bug?
//
public struct Position
{
    public enum Flag : byte
    {
        BlackTurn = 1 << 0,
        CastleRightsWQ = 1 << 1,
        CastleRightsWK = 1 << 2,
        CastleRightsBQ = 1 << 3,
        CastleRightsBK = 1 << 4,
        CastleRightsW = CastleRightsWQ | CastleRightsWK,
        CastleRightsB = CastleRightsBQ | CastleRightsBK,
    }
    
    public ulong[][] State;
    public ulong[] Occupancy;
    public Flag Flags;
    public ulong ZobristHash;
    byte _enPassantCapturable;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Side Us() => HasFlag(Flag.BlackTurn) ? Side.Black : Side.White;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Side Opp() => HasFlag(Flag.BlackTurn) ? Side.White : Side.Black;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Side OppOf(Side s) => 1 - s;

    public void GenerateOccupancy()
    {
        Occupancy[(int)Side.White] = State[(int)Side.White][(int)PieceType.Pawn] | State[(int)Side.White][(int)PieceType.Knight] |
                    State[(int)Side.White][(int)PieceType.Bishop] | State[(int)Side.White][(int)PieceType.Rook] |
                    State[(int)Side.White][(int)PieceType.Queen] | State[(int)Side.White][(int)PieceType.King];
        Occupancy[(int)Side.Black] = State[(int)Side.Black][(int)PieceType.Pawn] | State[(int)Side.Black][(int)PieceType.Knight] |
                         State[(int)Side.Black][(int)PieceType.Bishop] | State[(int)Side.Black][(int)PieceType.Rook] |
                         State[(int)Side.Black][(int)PieceType.Queen] | State[(int)Side.Black][(int)PieceType.King];
        Occupancy[(int)Side.Both] = Occupancy[(int)Side.White] | Occupancy[(int)Side.Black];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFlag(Flag f) => (Flags & f) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PieceType GetPieceType(Side s, int i)
    {
        foreach (var pt in PieceTypes.NotNone)
            if (BB.IsSet(State[(int)s][(int)pt], i)) return pt;
        return PieceType.None;
    }
    
    public int GetPieceType(int s, int i)
    {
        foreach (var pt in PieceTypes.NotNone)
            if (BB.IsSet(State[s][(int)pt], i)) return (int)pt;
        return (int)PieceType.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EnPassantCapturable() => _enPassantCapturable < 64 ? _enPassantCapturable : -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetEnPassantCapturable(int i) => _enPassantCapturable = i < 0 ? (byte)64 : (byte)i;

    public override string ToString()
    {
        string o = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            o += rank + 1 + "  ";
            for (int file = 0; file < 8; file++)
            {
                int i = BB.Index(file, rank);
                Side s = BB.IsSet(Occupancy[(int)Side.White], i) ? Side.White : Side.Black;
                PieceType pt = GetPieceType(s, i);
                char sym = "pnbrqk."[(int)pt];
                o += s == Side.White ? sym.ToString().ToUpper() : sym.ToString();
                o += "  ";
            }

            o += "\n";
        }

        o += "\n   a  b  c  d  e  f  g  h";

        return o;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong PawnPush(ulong bb) => Us() == Side.White ? bb << 8 : bb >> 8;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PawnPush() => PawnPush(Us());
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PawnPush(Side s) => s == Side.White ? 8 : -8;

    public void ApplyMove(Move move)
    {
        Move.Flag f = move.GetFlags();
        int pt = (int)move.GetPieceType();
        int us = (int)Us();
        int from = move.GetFrom();
        int to = move.GetTo();
        ulong fromSq = BB.FromIndex(from);
        ulong toSq = BB.FromIndex(to);
        ZobristHash ^= Zobrist.FlagHash(Flags); // Unset old flags and en passant square from hash.
        ZobristHash ^= Zobrist.EPCHash(EnPassantCapturable());

        if (pt == (int)PieceType.Pawn && Math.Abs(to-from) == 16)
            SetEnPassantCapturable(to);
        else SetEnPassantCapturable(-1);

        ZobristHash ^= Zobrist.EPCHash(EnPassantCapturable());
        // If this is a capturing move, remove opponent's piece.
        if (Move.HasFlag(f, Move.Flag.Capture | Move.Flag.EnPassant))
        {
            int opp = (int)Opp();
            int capIdx = to;
            
            if (Move.HasFlag(f, Move.Flag.EnPassant)) capIdx -= PawnPush(); // Capture the piece behind the pawn.
            State[opp][GetPieceType(opp, capIdx)] &= ~BB.FromIndex(capIdx);
            ZobristHash ^= Zobrist.PieceHash(opp, GetPieceType(opp, capIdx), capIdx);
            
            // Make captured position in opponent occupancy map empty.
            Occupancy[opp] &= ~BB.FromIndex(capIdx);
        }
        
        // Move our piece.
        State[us][pt] &= ~fromSq; // Remove our piece from its initial position.
        Occupancy[us] &= ~fromSq; // Make initial position in friendly side occupancy map empty.
        ZobristHash ^= Zobrist.PieceHash(us, pt, from);

        // Change piece type in case of promotion.
        if (Move.HasFlag(f, Move.Flag.Promotion)) pt = (int)move.GetPromotion();
        
        State[us][pt] |= toSq; // Place our piece in the new position.
        ZobristHash ^= Zobrist.PieceHash(us, pt, to);
        Occupancy[us] |= toSq; // Fill new position in friendly side occupancy map.

        if (Move.HasFlag(f, Move.Flag.Castle))
        {
            (ulong, ulong) fromTo = MoveGenerator.RookCastleSqs(to);
            
            // Remove rook
            State[us][(int)PieceType.Rook] &= ~fromTo.Item1;
            ZobristHash ^= Zobrist.PieceHash(us, (int)PieceType.Rook, BB.LSBIndex(fromTo.Item1));
            Occupancy[us] &= ~fromTo.Item1;
            
            // Place rook
            State[us][(int)PieceType.Rook] |= fromTo.Item2;
            ZobristHash ^= Zobrist.PieceHash(us, (int)PieceType.Rook, BB.LSBIndex(fromTo.Item2));
            Occupancy[us] |= fromTo.Item2;
            
            Flags &= us == (int)Side.White ? ~Flag.CastleRightsW : ~Flag.CastleRightsB;
        }

        switch (pt)
        {
            case (int)PieceType.King:
                Flags &= us == (int)Side.White ? ~Flag.CastleRightsW : ~Flag.CastleRightsB;
                break;
            case (int)PieceType.Rook:
                Flags &= ~MoveGenerator.RookCastleFlag(from);
                break;
        }

        Occupancy[(int)Side.Both] = Occupancy[us] | Occupancy[(int)Opp()];
        Flags ^= Flag.BlackTurn; // Flip turn flag.
        ZobristHash ^= Zobrist.FlagHash(Flags);
        ZobristHash = Zobrist.FullHash(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(Position other)
    {
        Buffer.BlockCopy(other.State[(int)Side.White], 0, State[(int)Side.White], 0, 48);
        Buffer.BlockCopy(other.State[(int)Side.Black], 0, State[(int)Side.Black], 0, 48);
        Buffer.BlockCopy(other.Occupancy, 0, Occupancy, 0, 24);
        _enPassantCapturable = other._enPassantCapturable;
        Flags = other.Flags;
        ZobristHash = other.ZobristHash;
    }
    
    public static Position Empty()
    {
        return new Position()
        {
            State = new [] { new ulong[6], new ulong[6] }, 
            Occupancy = new ulong[3],
        };
    }
    
    public static Position StartingPosition()
    {
        Position pos = new()
        {
            State = new [] { Array.Empty<ulong>(), Array.Empty<ulong>() },
            _enPassantCapturable = 64,
            Flags = Flag.CastleRightsWQ | Flag.CastleRightsWK | Flag.CastleRightsBQ | Flag.CastleRightsBK,
            Occupancy = new ulong[3],
        };
        pos.State[(int)Side.White] = new ulong[] {
            0b11111111UL << 8, // White pawns
            0b01000010UL, // White knights
            0b00100100UL, // White bishops
            0b10000001UL, // White rooks
            0b00001000UL, // White queen
            0b00010000UL, // White king
        };
        pos.State[(int)Side.Black] = new ulong[] {
            0b11111111UL << 48, // Black pawns
            0b01000010UL << 56, // Black knights
            0b00100100UL << 56, // Black bishops
            0b10000001UL << 56, // Black rooks
            0b00001000UL << 56, // Black queen
            0b00010000UL << 56, // Black king
        };
        pos.GenerateOccupancy();
        pos.ZobristHash = Zobrist.FullHash(pos);
        return pos;
    }

    public static Position FromFEN(string fen)
    {
        Position pos = Empty();

        string[] parts = fen.Split(' ');
        string[] ranks = parts[0].Split('/');
        Array.Reverse(ranks); // Needs to start from rank 1 and increase

        for (int r = 0; r < 8; ++r)
        {
            int f = 0;
            foreach (var c in ranks[r])
            {
                if (char.IsDigit(c))
                {
                    f += int.Parse(c.ToString());
                    continue;
                }
                PieceType pt;
                Side side;
                switch (char.ToLower(c))
                {
                    case 'p': pt = PieceType.Pawn; break;
                    case 'n': pt = PieceType.Knight; break;
                    case 'b': pt = PieceType.Bishop; break;
                    case 'r': pt = PieceType.Rook; break;
                    case 'q': pt = PieceType.Queen; break;
                    case 'k': pt = PieceType.King; break;
                    default: throw new Exception("Invalid character encountered in FEN string.");
                }
                side = char.IsLower(c) ? Side.Black : Side.White;
                pos.State[(int)side][(int)pt] |= BB.FromIndex(BB.Index(f, r));
                pos.Occupancy[(int)side] |= BB.FromIndex(BB.Index(f, r));
                f++;
            }
        }

        pos.GenerateOccupancy();

        pos.Flags |= parts[1] == "w" ? 0 : Flag.BlackTurn;
        if (parts[2].Contains("K")) pos.Flags |= Flag.CastleRightsWK;
        if (parts[2].Contains("Q")) pos.Flags |= Flag.CastleRightsWQ;
        if (parts[2].Contains("k")) pos.Flags |= Flag.CastleRightsBK;
        if (parts[2].Contains("q")) pos.Flags |= Flag.CastleRightsBQ;

        if (parts[3] != "-") pos.SetEnPassantCapturable(UCI.ToIndex(parts[3]));

        pos.ZobristHash = Zobrist.FullHash(pos);

        return pos;
    }
}


