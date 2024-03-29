﻿using System.Runtime.CompilerServices;

namespace Alexvis;

public class TranspositionTable
{
    public struct Entry
    {
        public int Value;
        public byte Depth;
        public Bound Type;
        public Move Move;
        public ulong ZobristHash;
    }
    public enum Bound : byte
    {
        Lower,
        Upper,
        Exact,
    }
    
    Entry[] _entries = new Entry[2097152]; // 2^21 entries, which is equivalent to: 14 bytes per entry * 2^23 = 117,44 MB.
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int TableIndex(ulong hash) => (int)(hash >> 43); // 64 - 21 = 43 bits.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Register(ulong hash, int value, int depth, Bound bound, Move move)
    {
        int i = TableIndex(hash);
        Entry old = _entries[i];
        
        // If entry exists but we are replacing a null move best move with a non-null best move replace anyways
        // even if the depth of the new move is lower.
        if (old.ZobristHash != 0 && !(old.Move.Equals(Move.NullMove) && !move.Equals(Move.NullMove)))
            if (old.Depth > depth) return; // Entry exists and depth is better than new entry.

        _entries[i].ZobristHash = hash;
        _entries[i].Depth = (byte)depth;
        _entries[i].Value = value;
        _entries[i].Type = bound;
        _entries[i].Move = move;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Lookup(ulong hash, out Entry e)
    {
        int i = TableIndex(hash);
        e = _entries[i];
        return e.ZobristHash == hash;
    }
}