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
            string[] parts = input.Split(' ');
            if (parts.Length == 0) continue;
            if (parts[0] == "uci")
            {
                Console.WriteLine("id name Alexvis\nid author C.F. Schilham\nuciok");
            }
            else if (parts[0] == "isready") Console.WriteLine("readyok");
            else if (parts.Length == 1 && parts[0] == "position") pos = Position.StartingPosition();
            else if (parts.Length >= 2 && parts[0] == "position" && parts[1] == "startpos") pos = Position.StartingPosition();
            else if (parts.Length > 2 && parts[0] == "position" && parts[1] == "fen")
                pos = Position.FromFEN(string.Join(' ', parts.Skip(2).Take(6)));
            else if (parts[0] == "go")
            {
                new Thread(() => { searcher.Search(pos, 40);}).Start();         
                new Thread(() =>
                {
                    Thread.Sleep(5000);
                    searcher.RequestStop();
                }).Start();
            }
            else if (parts[0] == "stop") searcher.RequestStop();
            if (parts.Length > 3 && parts[0] == "position" && parts.Contains("moves"))
            {
                foreach (var uciMove in parts.SkipWhile(p => p != "moves").Skip(1))
                {
                    List<Move> legalMoves = MoveGenerator.GenerateAllLegalMoves(pos);
                    Move move = UCI.SelectMove(uciMove, legalMoves);
                    pos.ApplyMove(move);
                }
            }
        }
    }
}
