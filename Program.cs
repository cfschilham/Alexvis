using System.Collections.Concurrent;

namespace Alexvis;

class Program
{
   
    public static void Main()
    {
        Position pos = Position.StartingPosition();
        TranspositionTable tt = new();
        Searcher searcher = new(tt);
        string input = "";
        while (input != "quit")
        {
            input = Console.ReadLine()!;
            List<string> parts = input.Split(' ').ToList();
            if (parts.Count == 0) continue;
            if (parts[0] == "uci")
            {
                Console.WriteLine("id name Alexvis\nid author C.F. Schilham\nuciok");
            }
            else if (parts[0] == "isready") Console.WriteLine("readyok");
            else if (parts[0] == "position")
            {
                searcher.ClearHistory();
                if (parts.Count == 1 || parts.Count > 1 && parts[1] == "startpos")
                {
                    pos = Position.StartingPosition();
                    searcher.AddHistory(pos, false);
                }
            }
            else if (parts.Count > 2 && parts[0] == "position" && parts[1] == "fen")
            {
                pos = Position.FromFEN(string.Join(' ', parts.Skip(2).Take(6)));
                searcher.AddHistory(pos, false);
            }
            else if (parts[0] == "go")
            {
                int? moveTime = null;
                int? maxDepth = null;
                int? wtime = null;
                int? btime = null;
                int? winc = null;
                int? binc = null;
                if (parts.Contains("movetime")) moveTime = int.Parse(parts[parts.IndexOf("movetime") + 1]);
                if (parts.Contains("depth")) maxDepth = int.Parse(parts[parts.IndexOf("depth") + 1]);
                if (parts.Contains("wtime")) wtime = int.Parse(parts[parts.IndexOf("wtime") + 1]);
                if (parts.Contains("btime")) btime = int.Parse(parts[parts.IndexOf("btime") + 1]);
                if (parts.Contains("winc")) winc = int.Parse(parts[parts.IndexOf("winc") + 1]);
                if (parts.Contains("binc")) binc = int.Parse(parts[parts.IndexOf("binc") + 1]);
                
                searcher.StartThinking(pos, Console.Out, moveTime, maxDepth, wtime, btime, winc, binc);
            }
            else if (parts[0] == "stop") searcher.RequestStop();
            if (parts.Count > 3 && parts[0] == "position" && parts.Contains("moves"))
            {
                foreach (var uciMove in parts.SkipWhile(p => p != "moves").Skip(1))
                {
                    List<Move> legalMoves = MoveGenerator.GenerateAllLegalMoves(pos);
                    Move move = UCI.SelectMove(uciMove, legalMoves);
                    pos.ApplyMove(move);
                    searcher.AddHistory(pos, move.GetPieceType() == PieceType.Pawn);
                }
            }
        }
    }
}
