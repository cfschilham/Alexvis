using System.Collections;
using System.Runtime.CompilerServices;

namespace BughouseChess.Core;

public class PositionList
{
    int _index = -1;
    int _capacity;
    Position[] _positions;

    public PositionList(int capacity)
    {
        _positions = new Position[capacity];
        _capacity = capacity;
        for (int i = 0; i < capacity; i++)
            _positions[i] = Position.Empty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLegalChildren(Position parent, List<Move> moves)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            _positions[++_index].CopyFrom(parent);
            _positions[_index].ApplyMove(moves[i]);
            if (!MoveGenerator.IsLegal(_positions[_index])) _index--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Length() => _index;
    
    public Position this[int i]
    {
        get => _positions[i];
        set => _positions[i] = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _index = -1;
}