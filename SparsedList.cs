using System;
using System.Runtime.CompilerServices;

namespace Necs
{
    public class SparsedList<T>
    {
        public int Lenght => _array.Length;

        T[] _array;

        public SparsedList(int capacity = 6)
        {
            _array = new T[capacity];
        }

        public void Set(int pos, T value)
        {
            if (pos < 0)
                return;

            EnsureCapacity(pos);

            _array[pos] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureCapacity(int size)
        {
            var lenght = _array.Length;
            if (lenght < size)
            {
                lenght = lenght << 1;

                if (lenght < size)
                    lenght = size;

                Array.Resize(ref _array, lenght);
            }
        }

        public T this[int index]
        {
            get => _array[index];
            set => Set(index, value);
        }
    }
}
