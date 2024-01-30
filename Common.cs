namespace Alexvis;

public enum PieceType
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King,
    None,
}

static class PieceTypes
{
    public static readonly int[] IntNotNone =
    {
        (int)PieceType.Pawn,
        (int)PieceType.Knight,
        (int)PieceType.Bishop,
        (int)PieceType.Rook,
        (int)PieceType.Queen,
        (int)PieceType.King,
    };
    public static readonly PieceType[] NotNoneOrKing =
    {
        PieceType.Pawn,
        PieceType.Knight,
        PieceType.Bishop,
        PieceType.Rook,
        PieceType.Queen,
    };
    public static readonly PieceType[] NotNone =
    {
        PieceType.Pawn,
        PieceType.Knight,
        PieceType.Bishop,
        PieceType.Rook,
        PieceType.Queen,
        PieceType.King,
    };
}

public enum Side
{
    White,
    Black,
    Both,
}

public static class Sides
{
    public static readonly Side[] NotBoth =
    {
        Side.White,
        Side.Black,
    };
    public static readonly int[] IntNotBoth =
    {
        (int)Side.White,
        (int)Side.Black,
    };
}

public struct Square
{
    public PieceType PieceType;
    public Side Side;
}

