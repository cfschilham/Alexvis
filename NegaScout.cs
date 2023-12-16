using System.Collections.Concurrent;

namespace BughouseChess.Core;

public struct TranspositionEntry
{
    public enum EntryType : byte
    {
        Lowerbound,
        Upperbound,
        Equal,
    }
    
    public int Value;
    public int Depth;
    public EntryType Type;
}

public static class NegaScout
{
    static readonly int[] MatVal = { 100, 305, 310, 500, 900 };
    static readonly Dictionary<ulong, TranspositionEntry> TranspositionTable = new();
    static volatile bool Stop;
    public static void RequestStop() => Stop = true;
    
    static int H(Position pos, int numMoves)
    {
        if (numMoves == 0)
        {
            if (MoveGenerator.IsChecked(pos))
                return int.MinValue;
            return 0;
        }
        
        int materialScore = 0;
        
        foreach (var side in Sides.All)
            foreach (var pt in PieceTypes.NotNone)
            {
                if (pt is PieceType.None or PieceType.King) continue;
                materialScore += MatVal[(int)pt] * Bitboard.PopCount(pos.State[(int)side][(int)pt]) * (pos.Us() == side ? 1 : -1);
            }

        return materialScore + numMoves;
    }

    // public static int Quiesce(Position pos, int depth, List<Move>[] mvAlloc, PositionList[] cldAlloc, int a, int b)
    // {
    //     
    // }
    
    // Possible optimizations: order child nodes.
    public static int Eval(Position pos, int depth, List<Move>[] mvAlloc, PositionList[] cldAlloc,
         int a = int.MinValue, int b = int.MaxValue)
    {
        int aOrig = a;
        
        TranspositionEntry te;
        if (TranspositionTable.TryGetValue(pos.TTHash(), out te) && te.Depth >= depth)
        {
            if (te.Type == TranspositionEntry.EntryType.Equal) return te.Value;
            if (te.Type == TranspositionEntry.EntryType.Lowerbound) a = Math.Max(a, te.Value);
            else if (te.Type == TranspositionEntry.EntryType.Upperbound) b = Math.Min(b, te.Value);
            
        
            if (a >= b) return te.Value;
        }
        
        List<Move> moves = mvAlloc[depth];
        moves.Clear();
        MoveGenerator.GenerateAllMoves(pos, moves);
        PositionList children = cldAlloc[depth];
        children.Clear();

        children.AddLegalChildren(pos, moves);
        
        if (Stop || depth == 0 || children.Length() == 0) 
            return H(pos, children.Length());

        int value = int.MinValue;
        for (int i = 0; i < children.Length(); i++)
        {
            value = Math.Max(value, -Eval(children[i], depth - 1, mvAlloc, cldAlloc, -b, -a));
            a = Math.Max(value, a);
            if (a >= b) break;
        }

        te.Value = value;
        if (value <= aOrig) te.Type = TranspositionEntry.EntryType.Upperbound;
        else if (value >= b) te.Type = TranspositionEntry.EntryType.Lowerbound;
        else te.Type = TranspositionEntry.EntryType.Equal;
        te.Depth = depth;
        TranspositionTable.Remove(pos.TTHash());
        TranspositionTable.Add(pos.TTHash(), te);

        return value;
    }

    // public static int Eval(Position pos, int depth, int a = int.MinValue, int b = int.MaxValue)
    // {
    //     List<Move>[] mvAlloc = new List<Move>[depth+1];
    //     PositionList[] cldAlloc = new PositionList[depth+1];
    //     for (int i = 0; i <= depth; i++)
    //     {
    //         mvAlloc[i] = new List<Move>(200);
    //         cldAlloc[i] = new PositionList(200);
    //     }
    //
    //     return Eval(pos, depth, mvAlloc, cldAlloc, a, b);
    // }

    public static void GenerateMoveUCI(Position pos, int maxDepth)
    {
        Stop = false;
        Position head = Position.Empty();
        List<Move> legalMoves = new(200);
        List<Move> moves = new(200);
        MoveGenerator.GenerateAllMoves(pos, moves);
        foreach (var move in moves)
        {
            head.CopyFrom(pos);
            head.ApplyMove(move);
            if (MoveGenerator.IsLegal(head))
                legalMoves.Add(move);
        }

        if (legalMoves.Count == 0) return;
        
        List<Move>[] mvAlloc = new List<Move>[maxDepth];
        PositionList[] cldAlloc = new PositionList[maxDepth];
        for (int i = 0; i < maxDepth; i++)
        {
            mvAlloc[i] = new List<Move>(200);
            cldAlloc[i] = new PositionList(200);
        }
        
        (int, Move)[] values = new (int, Move)[legalMoves.Count];
        (int, Move)[] oldValues = values;
        for (int depth = 0; depth < maxDepth; depth++)
        {
            int a = int.MinValue;
            int value;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                head.CopyFrom(pos);
                head.ApplyMove(legalMoves[i]);
                value = -Eval(head, depth, mvAlloc, cldAlloc, a);
                a = Math.Max(a, value);
                values[i] = (value, legalMoves[i]);
            }

            if (Stop) break;
            Array.Sort(values, (tuple, valueTuple) => tuple.Item1 > valueTuple.Item1 ? -1 : 1);
            Console.WriteLine($"info depth {depth+1} score cp {values[0].Item1} pv {Bitboard.MoveToUCI(values[0].Item2)}");
            oldValues = ((int, Move)[])values.Clone();
        }
        Console.WriteLine($"bestmove {Bitboard.MoveToUCI(oldValues[0].Item2)}");
        Stop = false;
    }
}