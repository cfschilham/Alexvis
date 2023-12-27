using System.Collections;
using System.Runtime.CompilerServices;

namespace Alexvis;

public class PositionStack
{
    int _readIndex = -1;
    int _capacity;
    readonly Position[] _positions;

    public PositionStack(int capacity)
    {
        _positions = new Position[capacity];
        _capacity = capacity;
        for (int i = 0; i < capacity; i++)
            _positions[i] = Position.Empty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(Position pos) => _positions[++_readIndex].CopyFrom(pos);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop(ref Position pos) => pos.CopyFrom(_positions[_readIndex--]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop() => _readIndex--;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyTop(ref Position pos) => pos.CopyFrom(_positions[_readIndex]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Size() => _readIndex;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _readIndex = -1;
}