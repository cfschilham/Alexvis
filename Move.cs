﻿using System.Runtime.CompilerServices;

namespace Alexvis;

public struct Move
{
    public static Move NullMove = new Move { _data = 0 };
    
    public enum Flag : byte
    {
        None = 0,
        Capture = 1 << 0,
        Castle = 1 << 1,
        Promotion = 1 << 2,
        EnPassant = 1 << 3,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFlag(Flag f) => (_data & (int)f) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag(Flag flags, Flag f) => (flags & f) != 0;
    
    uint _data;
    
    public Move(int from, int to, PieceType pt, Flag flags, PieceType promotion) =>
        _data += (uint)from << 26 | (uint)to << 20 | (uint)pt << 17 | (uint)promotion << 14 | (uint)flags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetFrom() => (int)(_data >> 26);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetTo() => (int)(_data >> 20 & 0b111111);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PieceType GetPieceType() => (PieceType)(_data >> 17 & 0b111);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Flag GetFlags() => (Flag)(_data & 0b1111);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PieceType GetPromotion() => (PieceType)(_data >> 14 & 0b111);
    
    public bool Equals(Move other) => _data == other._data;
    public override bool Equals(object? obj) => obj is Move other && Equals(other);
    public override int GetHashCode() => (int)_data;

    public override string ToString() => UCI.FromMove(this);
}
