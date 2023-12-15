using System.Collections.Concurrent;
using BughouseChess.Core;

class Program
{
    public static void Main()
    {
        Position pos = Position.StartingPosition();
        int bestValue;
        Move bestMove;
        MoveGenerator.Init();
        NegaScout.GenerateMove(pos, 6, out bestValue, out bestMove);
        Console.WriteLine($"bestmove: {bestMove.GetFrom()}, {bestMove.GetTo()}, {bestMove.GetFlags()}, {bestMove.GetPromotion()}, value: {bestValue}");
        // List<Move> moves = new(200);
        // MoveGenerator.GenerateAllMoves(pos, moves);
        // foreach (var move in moves)
        // {
        //     Console.WriteLine($"{move.GetFrom()}, {move.GetTo()}, flags {move.GetFlags()}, promo {move.GetPromotion()}");
        // }
    }
}
