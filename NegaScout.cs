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
    static readonly ConcurrentDictionary<ulong, TranspositionEntry> TranspositionTable = new();
    
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
                materialScore += MatVal[(int)pt] * pos.State[(int)side][(int)pt].PopCount() * (pos.Us() == side ? 1 : -1);
            }

        return materialScore + numMoves;
    }
    
    // Possible optimizations: order child nodes.
    public static int Eval(Position pos, int depth, List<Move>[] mvAlloc, List<Position>[] cldAlloc,
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
        Position head;
        List<Position> children = cldAlloc[depth];
        children.Clear();

        foreach (var move in moves)
        {
            head = pos.DeepClone();
            head.ApplyMove(move);
            if (MoveGenerator.IsLegal(head))
                children.Add(head);
        }
        
        if (depth == 0 || children.Count == 0) 
            return H(pos, children.Count);

        int value = int.MinValue;
        foreach (var child in children)
        {
            value = Math.Max(value, -Eval(child, depth - 1, mvAlloc, cldAlloc, -b, -a));
            a = Math.Max(value, a);
            if (a >= b) break;
        }

        te.Value = value;
        if (value <= aOrig) te.Type = TranspositionEntry.EntryType.Upperbound;
        else if (value >= b) te.Type = TranspositionEntry.EntryType.Lowerbound;
        else te.Type = TranspositionEntry.EntryType.Equal;
        te.Depth = depth;
        TranspositionTable.AddOrUpdate(pos.TTHash(), te, (_, _) => te);

        return value;
    }

    public static int Eval(Position pos, int depth)
    {
        List<Move>[] mvAlloc = new List<Move>[depth+1];
        List<Position>[] cldAlloc = new List<Position>[depth+1];
        for (int i = 0; i <= depth; i++)
        {
            mvAlloc[i] = new List<Move>(200);
            cldAlloc[i] = new List<Position>(200);
        }

        return Eval(pos, depth, mvAlloc, cldAlloc);
    }

    public static void GenerateMove(Position pos, int depth, out int bestValue, out Move bestMove)
    {
        Position head;
        List<Move> legalMoves = new(200);
        List<Move> moves = new(200);
        MoveGenerator.GenerateAllMoves(pos, moves);
        foreach (var move in moves)
        {
            head = pos.DeepClone();
            head.ApplyMove(move);
            if (MoveGenerator.IsLegal(head))
                legalMoves.Add(move);
        }
        if (legalMoves.Count == 0)
        {
            bestMove = new Move(0, 0, PieceType.None, 0, 0);
            bestValue = H(pos, 0);
            return;
        }
        
        ConcurrentBag<(Move, int)> results = new ();
        Parallel.ForEach(legalMoves, lm =>
        {
            Position child = pos.DeepClone();
            child.ApplyMove(lm);
            results.Add((lm, -Eval(child, depth-1)));
        });
        
        bestValue = int.MinValue;
        bestMove = legalMoves[0];
        foreach (var res in results)
        {
            if (res.Item2 > bestValue)
            {
                bestMove = res.Item1;
                bestValue = res.Item2;
            }
        }

        bestValue *= pos.Us() == Side.White ? 1 : -1;
    }
}