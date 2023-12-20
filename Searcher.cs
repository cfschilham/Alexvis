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
        Span<Move> moves = stackalloc Move[200];
        bool isChecked = MoveGenerator.IsChecked(pos);
        if (!isChecked)
        {
            int stnd = Score.Static(pos, null, -1);
            if (stnd >= b) return stnd;
            a = Math.Max(stnd, a);
            
            mslen = MoveGenerator.GenerateCapturesAndPromotions(pos, moves);
            if (mslen == 0) return stnd;
        }
        else mslen = MoveGenerator.GenerateAllMoves(pos, moves);
        
        ps.Push(pos);
        int value = int.MinValue;
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
            value = Math.Max(value, -Quiesce(pos, depth - 1, ply + 1, ps, -b, -a));
            ps.ApplyTop(ref pos);
            a = Math.Max(a, value);
            if (value >= b)
            {
                ps.Pop();
                return value;
            }
        }
        ps.Pop();
        
        // Stalemate can't be detected because if we're not in check, we don't generate all moves and thus we don't
        // perform a full search.
        if (numChildren == 0 && isChecked) Score.FromMatePly(ply);
        return value;
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
        int value = int.MinValue;
        Move bestMove = Move.NullMove;
        TranspositionTable.Bound bound = TranspositionTable.Bound.Upper;
        for (int i = 0; i < mslen && !Stop; i++)
        {
            pos.ApplyMove(moves[i]);
            if (!MoveGenerator.IsLegal(pos))
            {
                ps.ApplyTop(ref pos);
                continue;
            }
            numChildren++;
            value = Math.Max(value, -Eval(pos, depth - 1, ps, ply + 1, -b, -a));
            ps.ApplyTop(ref pos);

            
            if (value > a) // New best (PV) node has been found.
            {
                a = value;
                bestMove = moves[i];
                bound = TranspositionTable.Bound.Exact;
            }
            if (Stop) depth = 1;
            if (value >= b) // Beta cutoff
            {
                tt.Register(pos.ZobristHash, b, depth, TranspositionTable.Bound.Lower, moves[i]);
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
        
        tt.Register(pos.ZobristHash, value, depth, bound, bestMove);
        return value;
    }

    public void Search(Position pos, int maxDepth, TextWriter tw)
    {
        Stop = false;
        nodesSearched = 0;
        List<Move> moves = MoveGenerator.GenerateAllLegalMoves(pos);
        if (moves.Count == 0) return;

        PositionStack ps = new(64);
        ps.Push(pos);
        List<TranspositionTable.Entry> pv = new();
        for (int depth = 0; depth <= maxDepth && !Stop; depth++)
        {
            Eval(pos, depth, ps);
            ps.ApplyTop(ref pos);
            pv = TracePV(pos, ps);

            if (pv.Count > 0)
            {
                string score = $"cp {pv[0].Value}";
                if (Score.IsMate(pv[0].Value))
                {
                    int matePly = Score.ToMatePly(pv[0].Value);
                    int mateMove = matePly / 2 + matePly % 2;
                    if (pv[0].Value < 0) mateMove = -mateMove;
                    else Stop = true; // Stop after finding forced mate for us.
                    score = $"mate {mateMove}";
                }
                tw.WriteLine($"info nodes {nodesSearched} depth {depth} score {score} pv {string.Join(' ', pv.Select(e => UCI.FromMove(e.Move)))}");
            }
        }
        // pv = TracePV(pos, ps);
        // tw.WriteLine($"info nodes {nodesSearched}");
        tw.WriteLine($"bestmove {UCI.FromMove(pv[0].Move)}");
        Stop = false;
    }

    List<TranspositionTable.Entry> TracePV(Position pos, PositionStack ps)
    {
        List<TranspositionTable.Entry> pv = new (50);
        ps.Push(pos);
        while (tt.Lookup(pos.ZobristHash, out var head) && head.Type != TranspositionTable.Bound.Upper && pv.Count <= 50)
        {
            pv.Add(head);
            pos.ApplyMove(head.Move);
        }
        ps.Pop(ref pos);
        return pv;
    }

    public void StartThinking(Position pos, TextWriter tw, int? moveTime, int? maxDepth, int? wtime, int? btime, int? winc, int? binc)
    {
        maxDepth ??= 40;
        int? time = pos.Us() == Side.White ? wtime : btime;
        int? inc = pos.Us() == Side.White ? winc : binc;
        
        if (moveTime != null && moveTime != 0) time = moveTime; // Fixed time was set.
        else if (time != null && time != 0) // Game has time control, calculate time to use.
        {
            time /= 800; // Estimate that a game will last 40 moves.
            time *= MoveGenerator.GenerateAllLegalMoves(pos).Count; // At 40 legal moves, time = 1/40 of our time.
            if (inc != null) time += inc / 2; // Use half of our increment as well.
        }
        time ??= int.MaxValue; // No fixed move time and no time control, search indefinitely until depth is reached.

        new Thread(() => { Search(pos, (int)maxDepth, tw); }).Start();
        if (time != int.MaxValue) new Thread(() =>
        {
            Thread.Sleep((int)time);
            RequestStop();
        }).Start();
    }
}