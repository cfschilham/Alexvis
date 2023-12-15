namespace BughouseChess.Core;

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
// Possible optimizations:
// - Hard-coded legal move masks for every piece.
// - Hard-coded GetFile without expensive modulo operation.
//
public struct Position
{
    public ulong TTHash()
    {
        var res = 0xcbf29ce484222325; // 64 bit FNV_offset_basis
        unchecked
        {
            foreach (Bitboard[] side in State)
                foreach (Bitboard bb in side)
                    res = (res ^ bb) * 0x100000001b3; // 64 bit FNV_prime
            return res ^ (ulong)_flags ^ _enPassantCapturable;
        }
    }
    
    [Flags] 
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
    
    public Bitboard[][] State;
    public Bitboard[] Occupancy;
    Flag _flags;
    byte _enPassantCapturable;
    
    public Side Us() => HasFlag(Flag.BlackTurn) ? Side.Black : Side.White;
    public Side Opp() => 1 - Us();
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
    
    public bool HasFlag(Flag f) => (_flags & f) != 0;
    
    public PieceType GetPieceType(Side s, int i)
    {
        foreach (var pt in PieceTypes.NotNone)
            if (State[(int)s][(int)pt].IsSet(i)) return pt;
        return PieceType.None;
    }

    public int? EnPassantCapturable() => _enPassantCapturable < 64 ? _enPassantCapturable : null;

    void SetEnPassantCapturable(int? i)
    {
        _enPassantCapturable = i != null ? (byte)i : (byte)64;
    }

    public override string ToString()
    {
        string o = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            o += rank + 1 + "  ";
            for (int file = 0; file < 8; file++)
            {
                int i = Bitboard.Index(file, rank);
                Side s = Occupancy[(int)Side.White].IsSet(i) ? Side.White : Side.Black;
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

    public Bitboard PawnPush(Bitboard bb) => Us() == Side.White ? bb << 8 : bb >> 8;
    public int PawnPush() => PawnPush(Us());
    public static int PawnPush(Side s) => s == Side.White ? 8 : -8;

    public void ApplyMove(Move move)
    {
        Move.Flag f = move.GetFlags();
        PieceType pt = move.GetPieceType();
        int from = move.GetFrom();
        int to = move.GetTo();

        if (pt == PieceType.Pawn && Math.Abs(to-from) > 8)
            SetEnPassantCapturable(to);
        else SetEnPassantCapturable(null);
        // If this is a capturing move, remove opponent's piece.
        if (Move.HasFlag(f, Move.Flag.Capture | Move.Flag.EnPassant))
        {
            int captured = to;
            if (Move.HasFlag(f, Move.Flag.EnPassant)) captured -= PawnPush(); // Capture the piece behind the pawn.
            State[(int)Opp()][(int)GetPieceType(Opp(), captured)] ^= Bitboard.FromIndex(captured);
            
            // Make captured position in opponent occupancy map empty.
            Occupancy[(int)Opp()] &= ~Bitboard.FromIndex(captured);
        }
        
        // Move our piece.
        State[(int)Us()][(int)pt] ^= Bitboard.FromIndex(from); // Remove our piece from its initial position.

        // Change piece type in case of promotion.
        if (Move.HasFlag(f, Move.Flag.Promotion)) pt = move.GetPromotion();
        
        State[(int)Us()][(int)pt] |= Bitboard.FromIndex(to); // Place our piece in the new position.

        if (Move.HasFlag(f, Move.Flag.Castle))
        {
            (int, int) fromTo = MoveGenerator.RookCastleMove(to);
            
            // Remove rook
            State[(int)Us()][(int)PieceType.Rook] ^= Bitboard.FromIndex(fromTo.Item1);
            Occupancy[(int)Side.Both] &= ~Bitboard.FromIndex(fromTo.Item1);
            Occupancy[(int)Us()] &= ~Bitboard.FromIndex(fromTo.Item1);
            
            // Place rook
            State[(int)Us()][(int)PieceType.Rook] |= Bitboard.FromIndex(fromTo.Item2);
            Occupancy[(int)Side.Both] |= Bitboard.FromIndex(fromTo.Item2);
            Occupancy[(int)Us()] |= Bitboard.FromIndex(fromTo.Item2);
            
            _flags &= Us() == Side.White ? ~Flag.CastleRightsW : ~Flag.CastleRightsB;
        }

        switch (move.GetPieceType())
        {
            case PieceType.King:
                _flags &= Us() == Side.White ? ~Flag.CastleRightsW : ~Flag.CastleRightsB;
                break;
            case PieceType.Rook:
                _flags &= ~MoveGenerator.RookCastleFlag(from);
                break;
        }
        
        // Make initial position in general occupancy map empty.
        Occupancy[(int)Side.Both] &= ~Bitboard.FromIndex(from);
        
        // Make initial position in friendly side occupancy map empty.
        Occupancy[(int)Us()] &= ~Bitboard.FromIndex(from);
        
        // Fill new position in friendly side occupancy map.
        Occupancy[(int)Us()] |= Bitboard.FromIndex(to);
        
        // Fill new position in general occupancy map.
        Occupancy[(int)Side.Both] |= Bitboard.FromIndex(to);

        _flags ^= Flag.BlackTurn; // Flip turn flag.
    }

    public Position DeepClone()
    {
        Position clone = this with
        {
            State = new [] { Array.Empty<Bitboard>(), Array.Empty<Bitboard>() }, 
            Occupancy = new Bitboard[3],
        };
        foreach (var s in Sides.All)
        {
            clone.State[(int)s] = new Bitboard[6];
            foreach (var pt in PieceTypes.NotNone)
                clone.State[(int)s][(int)pt] = State[(int)s][(int)pt];
        }
        for (int i = 0; i < Occupancy.Length; i++)
            clone.Occupancy[i] = Occupancy[i];
        return clone;
    }
    
    public static Position StartingPosition()
    {
        Position p = new()
        {
            State = new [] { Array.Empty<Bitboard>(), Array.Empty<Bitboard>() },
            _enPassantCapturable = 64,
            _flags = Flag.CastleRightsWQ | Flag.CastleRightsWK | Flag.CastleRightsBQ | Flag.CastleRightsBK,
            Occupancy = new Bitboard[3],
        };
        p.State[(int)Side.White] = new Bitboard[] {
            0b11111111UL << 8, // White pawns
            0b01000010UL, // White knights
            0b00100100UL, // White bishops
            0b10000001UL, // White rooks
            0b00001000UL, // White queen
            0b00010000UL, // White king
        };
        p.State[(int)Side.Black] = new Bitboard[] {
            0b11111111UL << 48, // Black pawns
            0b01000010UL << 56, // Black knights
            0b00100100UL << 56, // Black bishops
            0b10000001UL << 56, // Black rooks
            0b00001000UL << 56, // Black queen
            0b00010000UL << 56, // Black king
        };
        p.GenerateOccupancy();
        return p;
    }
}


