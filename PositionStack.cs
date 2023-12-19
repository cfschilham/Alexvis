using System.Collections;
using System.Runtime.CompilerServices;

namespace Alexvis;

public class PositionStack
{
    int _index = -1;
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
    public void Push(Position pos) => _positions[++_index].CopyFrom(pos);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop(ref Position pos) => pos.CopyFrom(_positions[_index--]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop() => _index--;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyTop(ref Position pos) => pos.CopyFrom(_positions[_index]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Size() => _index;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _index = -1;
}