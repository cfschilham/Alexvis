using System.Collections.Concurrent;

namespace ArexMotor;

class Program
{
    // public static void Main()
    // {
    //     Position pos = Position.FromFEN("r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQ1RK1 b kq - 3 5");
    //     MoveGenerator.Init();
    //     Console.WriteLine("tables initialized");
    //     DateTime start = DateTime.Now;
    //     // Console.WriteLine(NegaScout.Eval(pos, 6));
    //     var res = NegaScout.GenerateMove(pos, 5);
    //     Console.WriteLine($"best move (value {res.Item1}) {res.Item2.GetFrom()} {res.Item2.GetTo()}, flags {res.Item2.GetFlags()}, promo {res.Item2.GetPromotion()}");
    //     Console.WriteLine($"{DateTime.Now.Subtract(start).TotalMilliseconds} ms");
    //     // List<Move> moves = new(200);
    //     // MoveGenerator.GenerateAllMoves(pos, moves);
    //     // foreach (var move in moves)
    //     // {
    //     //     Console.WriteLine($"{move.GetFrom()}, {move.GetTo()}, flags {move.GetFlags()}, promo {move.GetPromotion()}");
    //     // }
    // }
    
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
                Console.WriteLine("id name ArexMotor\nid author C.F. Schilham\nuciok");
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
