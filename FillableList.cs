using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Necs
{
    /// <summary>
    /// Simple List that instead of rearrange the array, keeps the emptied spaces
    /// and fills them automatically on add
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FillableList<T>
    {
        public const int DefaultFillableSpaces = 100;

        public T[] Buffer;

        public bool Invalidate = true;

        public int Count => _count;

        int _capacity;
        int _count;
        Stack<int> _fillable = new Stack<int>(DefaultFillableSpaces);

        public FillableList() : this(6)
        { }

        public FillableList(int capacity)
        {
            Buffer = new T[capacity];
            _capacity = capacity;
        }

        public int? Peek()
        {
            if (_fillable.Count == 0)
                return null;
            return _fillable.Peek();
        }

        public int Add(T value)
        {
            int pos;
            if (_fillable.Count != 0)
                pos = _fillable.Pop();
            else pos = _count++;

            Buffer[pos] = value;
            return pos;
        }

        public (bool, T) TryGet(int index)
        {
            if (index < 0 || index > _count || _fillable.Contains(index))
                return (false, default);

            return (true, Buffer[index]);
        }

        public (int, T) Remove(T value)
        {
            int index = Array.IndexOf(Buffer, value, 0, _count);

            return RemoveAt(index);
        }

        public (int, T) RemoveAt(int pos)
        {
            if (pos >= _count || pos < 0 || _fillable.Contains(pos))
                return (-1, default);

            _fillable.Push(pos);
            var temp = Buffer[pos];
            
            if (Invalidate)
                Buffer[pos] = default;

            return (pos, temp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureCapacity(int capacity)
        {
            if (_capacity < capacity)
            {
                _capacity = _capacity << 1;

                if (_capacity < capacity)
                    _capacity = capacity;

                Array.Resize(ref Buffer, _capacity);
            }
        }
    }
}
