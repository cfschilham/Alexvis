using System.Runtime.CompilerServices;

namespace Alexvis;

public class RepetitionStack(int capacity)
{
    // Index of the last position which resulted from an irreversible move, such as a pawn move or capture. This helps
    // with efficiency. We do not need to search anything that came before this position.
    int _lastIrreversible;
    readonly ulong[] _hashes = new ulong[capacity];
    readonly int[] _irreversibles = new int[capacity];
    int _readIndex = -1;

    public bool IsRepeated(ulong hash, int ply)
    {
        // If it's less than 5 ply since the last irreversible move, such as a pawn move, then this position cannot
        // possibly have been reached 3 times before.
        if (ply - _lastIrreversible < 5) return false;

        int reps = 0;
        
        // Loop over all items starting from the last irreversible move.
        for (int i = _lastIrreversible; i <= _readIndex; i++)
            if (hash == _hashes[i]) reps++;
        return reps >= 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(ulong hash, bool irreversible)
    {
        _hashes[++_readIndex] = hash;
        _irreversibles[_readIndex] = _lastIrreversible;
        if (irreversible) _lastIrreversible = _readIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop() => _lastIrreversible = _irreversibles[_readIndex--];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _lastIrreversible = 0;
        _readIndex = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Length() => _readIndex + 1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIrreversible(Move move) => move.GetPieceType() == PieceType.Pawn || move.GetFlags() != Move.Flag.None;

}