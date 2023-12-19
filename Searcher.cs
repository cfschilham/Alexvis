using System.Collections.Concurrent;

namespace Alexvis;

public class Searcher(TranspositionTable tt)
{
    int nodesSearched;
    volatile bool Stop;
    public void RequestStop() => Stop = true;

    public int Quiesce(Position pos, int depth, int ply, PositionStack ps, int a, int b)
    {
        nodesSearched++;
        if (depth == 0) return Score.Static(pos, null, -1);
        
        int mslen;
        Span <Move> moves = stackalloc Move[200];
        bool isChecked = MoveGenerator.IsChecked(pos);
        if (!isChecked)
        {
            int stnd = Score.Static(pos, null, -1);
            if (stnd >= b) return b;
            if (stnd > a) a = stnd;
            
            mslen = MoveGenerator.GenerateCapturesAndPromotions(pos, moves);
            if (mslen == 0) return a;
        }
        else mslen = MoveGenerator.GenerateAllMoves(pos, moves);
        
        ps.Push(pos);
        int value;
        int numChildren = 0;
        for (int i = 0; i < mslen; i++)
        {
            pos.ApplyMove(moves[i]);
            if (!MoveGenerator.IsLegal(pos))
            {
                ps.ApplyTop(ref pos);
                continue;
            }

            numChildren++;
            value = -Quiesce(pos, depth - 1, ply + 1, ps, -b, -a);
            ps.ApplyTop(ref pos);
            if (value > a) a = value;
            if (value >= b)
            {
                ps.Pop();
                return b;
            }
        }
        ps.Pop();
        
        // Stalemate can't be detected because if we're not in check, we don't generate all moves and thus we don't
        // perform a full search.
        if (numChildren == 0 && isChecked) Score.FromMatePly(ply);
        return a;
    }

    public void OrderMoves(Move prevBest, Span<Move> moves, int len)
    {
        for (int i = 0; i < len; i++)
        {
            if (moves[i].Equals(prevBest))
            {
                (moves[0], moves[i]) = (moves[i], moves[0]);
                break;
            }
        }
    }
    
    // Possible optimizations: order child nodes.
    public int Eval(Position pos, int depth, PositionStack ps, int ply = 0, int a = int.MinValue+1, int b = int.MaxValue)
    {
        nodesSearched++;

        bool ok = tt.Lookup(pos.ZobristHash, out var te);
        if (ok && te.Depth >= depth)
        {
            if (te.Type == TranspositionTable.Bound.Exact) return te.Value;
            if (te.Type == TranspositionTable.Bound.Lower) a = Math.Max(a, te.Value);
            else if (te.Type == TranspositionTable.Bound.Upper) b = Math.Min(b, te.Value);
            if (a >= b) return te.Value;
        }
        
        Span<Move> moves = stackalloc Move[200];
        int mslen = MoveGenerator.GenerateAllMoves(pos, moves);
        
        if (depth == 0) 
            return Quiesce(pos, 15, ply, ps, a, b);
        
        if (ok && !te.Move.Equals(Move.NullMove)) OrderMoves(te.Move, moves, mslen);
        
        ps.Push(pos);
        int numChildren = 0;
        int value = 0;
        Move bestMove = Move.NullMove;
        TranspositionTable.Bound bound = TranspositionTable.Bound.Upper;
        for (int i = 0; i < mslen; i++)
        {
            if (Stop)
            {
                ps.Pop();
                return Score.Static(pos, moves, mslen);
            }
            
            pos.ApplyMove(moves[i]);
            if (!MoveGenerator.IsLegal(pos))
            {
                ps.ApplyTop(ref pos);
                continue;
            }
            numChildren++;
            value = -Eval(pos, depth - 1, ps, ply + 1, -b, -a);
            ps.ApplyTop(ref pos);

            if (value > a) // New best (PV) node has been found.
            {
                a = value;
                bestMove = moves[i];
                bound = TranspositionTable.Bound.Exact;
            }
            if (value >= b) // Beta cutoff
            {
                if (!Stop) tt.Register(pos.ZobristHash, b, depth, TranspositionTable.Bound.Lower, moves[i]);
                ps.Pop();
                return value;
            }
        }
        ps.Pop();
        if (numChildren == 0)
        {
            // If there are no legal moves and current side is in check, it's checkmate. The value of this node will
            // already be set to int.MinValue, because it was never overwritten.
            return MoveGenerator.IsChecked(pos) ? Score.FromMatePly(ply) : 0; // If not in check, it's stalemate.
        }
        
        if (!Stop) tt.Register(pos.ZobristHash, value, depth, bound, bestMove);
        return value;
    }

    public void Search(Position pos, int maxDepth)
    {
        Stop = false;
        nodesSearched = 0;
        List<Move> moves = MoveGenerator.GenerateAllLegalMoves(pos);
        if (moves.Count == 0) return;

        PositionStack ps = new(64);
        ps.Push(pos);
        List<TranspositionTable.Entry> pv = new();
        for (int depth = 0; depth <= maxDepth; depth++)
        {
            Eval(pos, depth, ps);
            ps.ApplyTop(ref pos);
            pv = TracePV(pos, ps);
            if (Stop) break;

            if (pv.Count > 0)
            {
                string score = $"cp {pv[0].Value}";
                if (Score.IsMate(pv[0].Value))
                {
                    int matePly = Score.ToMatePly(pv[0].Value);
                    int mateMove = matePly / 2 + matePly % 2;
                    score = $"mate {mateMove}";
                }
                Console.WriteLine($"info nodes {nodesSearched} depth {depth} score {score} pv {string.Join(' ', pv.Select(e => UCI.FromMove(e.Move)))}");
            }
        }
        Console.WriteLine($"info nodes {nodesSearched}");
        Console.WriteLine($"bestmove {UCI.FromMove(pv[0].Move)}");
        Stop = false;
    }

    List<TranspositionTable.Entry> TracePV(Position pos, PositionStack ps)
    {
        List<TranspositionTable.Entry> pv = new (50);
        ps.Push(pos);
        while (tt.Lookup(pos.ZobristHash, out var head) && head.Type == TranspositionTable.Bound.Exact && pv.Count <= 50)
        {
            pv.Add(head);
            pos.ApplyMove(head.Move);
        }
        ps.Pop(ref pos);
        return pv;
    }
}