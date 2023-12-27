namespace Alexvis;

public class RepetitionStack(int capacity)
{
    int _lastIrreversible;
    readonly ulong[] _hashes = new ulong[capacity];
    readonly int[] _irreversibles = new int[capacity];
    int _readIndex = -1;

    public bool IsRepeated(ulong hash, int ply)
    {
        // If it's less than 4 ply since the last irreversible move, such as a pawn move, then this position cannot
        // possibly have been reached 3 times before.
        if (ply - _irreversibles[_readIndex] < 4) return false;

        for (int i = 0; i <= _readIndex; i++)
            if (hash == _hashes[i]) return true;
        return false;
    }

    public void Push(ulong hash, bool irreversible)
    {
        _hashes[++_readIndex] = hash;
        _irreversibles[_readIndex] = irreversible ? _readIndex : _lastIrreversible;
    }
    public void Pop() => _lastIrreversible = --_readIndex >= 0 ? _irreversibles[_readIndex] : 0;
}