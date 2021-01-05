using System;
using System.Runtime.CompilerServices;

namespace Necs
{
    internal interface IComponentPool
    {
        void Delete(int index);

        void Delete(Entity entity);

        bool Has(Entity entity);

        int Count { get; }

        Type Type { get; }
    }

    internal class ComponentPool<T> : IComponentPool
    {
        public const int ComponentsArrayDefaultSize = 1024;

        public Type Type { get; }

        internal FillableList<T> _components;
        internal SparsedList<int?> _packedEntities;
        internal SparsedList<int?> _sparsedEntities;

        T Default = default;

        public int Count => _components.Count;

        public ComponentPool(uint capacity = ComponentsArrayDefaultSize)
        {
            Type = typeof(T);
            var size = (int)capacity;
            _components = new FillableList<T>(size);
            _packedEntities = new SparsedList<int?>(size);
            _sparsedEntities = new SparsedList<int?>(size);
        }

        public int Add(Entity e, T value)
        {
            if (!typeof(T).Equals(Type))
                throw new Exception("Tried to add component to list with different type");

            int? exists = _sparsedEntities[e.Index];
            if (exists.HasValue)
                return exists.Value;

            int pos = _components.Add(value);
            _sparsedEntities[e.Index] = pos;
            _packedEntities[pos] = e.Index;

            return pos;
        }

        public int? GetComponentPos(Entity e) => _sparsedEntities[e.Index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity e) => _sparsedEntities[e.Index].HasValue;

        public (bool, T) Get(Entity e)
        {
            int? position = _sparsedEntities[e.Index];
            if (!position.HasValue)
                return (false, default);

            return (true, _components.Buffer[position.Value]);
        }

        public ref T GetRef(Entity e)
        {
            int? position = _sparsedEntities[e.Index];
            if (!position.HasValue)
                return ref Default;

            return ref _components.Buffer[position.Value];
        }

        /// <summary>
        /// Gets whether the component is valid and the component or it's default
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public (bool, T) Get(int index) => _components.TryGet(index);

        /// <summary>
        /// Removes the entity's component
        /// </summary>
        /// <param name="e">The entity to remove the component</param>
        /// <returns>whether the component existed and was removed, and the removed component or the default if it didn't exist</returns>
        public (bool, T) Remove(Entity e)
        {
            int? pos = _sparsedEntities[e.Index];
            if (!pos.HasValue)
                return (false, default);

            _sparsedEntities[e.Index] = null;
            _packedEntities[pos.Value] = null;
            var (_, val) = _components.RemoveAt(pos.Value);

            return (true, val);
        }


        /// <summary>
        /// Removes the component at the given index
        /// </summary>
        /// <param name="index">the component index</param>
        /// <returns>whether the component existed and was removed, and the removed component or the default if it didn't exist</returns>
        public (bool, T) RemoveAt(int index)
        {
            var (pos, val) = _components.RemoveAt(index);
            if (pos == -1)
                return (false, val);

            int? entityIndex = _packedEntities[pos];
            if (entityIndex.HasValue)
            {
                _sparsedEntities[entityIndex.Value] = null;
                _packedEntities[pos] = null;
            }

            return (true, val);
        }

        public void Delete(int index) => RemoveAt(index);

        public void Delete(Entity entity) => Remove(entity);

        internal T this[int index] => _components.Buffer[index];
    }
}
