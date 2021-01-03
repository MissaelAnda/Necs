using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Necs
{
    public struct Entity
    {
        public readonly long Id;

        public int Index => (int)(Id >> 32);

        public int Version => (int)(Id);

        public Entity(int index, int version)
        {
            Id = ((long)index << 32 | (long)version);
        }

        public bool IsValid() => IsValidEntity(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidEntity(Entity entity)
        {
            return (entity.Id >> 32) != (int)-1;
        }

        public static bool operator ==(Entity a, Entity b) => a.Id == b.Id;

        public static bool operator !=(Entity a, Entity b) => a.Id != b.Id;
    }

    public class World
    {
        internal FillableList<Entity> _entities = new FillableList<Entity>(1024);
        internal List<IComponentPool> _componentPools = new List<IComponentPool>();
        internal Dictionary<Type, int> _componentPoolIndices = new Dictionary<Type, int>();

        int _generationId = 0;

        bool _started = false;

        public World()
        {
            _entities.Invalidate = false;
        }

        ComponentPool<T> GetPoolOrCreate<T>()
        {
            var pool = GetPool<T>();
            if (pool != null)
                return pool;

            pool = new ComponentPool<T>();
            _componentPoolIndices.Add(typeof(T), _componentPools.Count);
            _componentPools.Add(pool);
            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ComponentPool<T> GetPool<T>()
        {
            var type = typeof(T);
            if (_componentPoolIndices.TryGetValue(type, out var index))
                return (ComponentPool<T>)_componentPools[index];

            return null;
        }

        #region API

        public Entity CreateEntity()
        {
            var index = _entities.Peek();
            Entity e;
            if (index.HasValue)
                e = new Entity(index.Value, _entities.Buffer[index.Value].Version + 1);
            else
                e = new Entity(_entities.Count, 0);

            _entities.Add(e);
            return e;
        }

        #region CreateWith<T1..., T16>

        //public int CreateWith<T1>()
        //{
        //    return _generationId++;
        //}

        #endregion

        public void DeleteEntity(Entity entity)
        {
            var index = entity.Index;
            if (_entities.Buffer[index] != entity)
                return;

            for (int i = 0; i < _componentPools.Count; i++)
                _componentPools[i].Delete(entity);

            _entities.RemoveAt(entity.Index);
        }

        #region AddComponent<T1.., T16>

        public T AddComponent<T>(Entity entity) where T : new()
        {
            var pool = GetPoolOrCreate<T>();

            int pos = pool.Add(entity, new T());

            return pool._components.Buffer[pos];
        }

        public ref T AddComponentRef<T>(Entity entity) where T : new()
        {
            var pool = GetPoolOrCreate<T>();

            int pos = pool.Add(entity, new T());

            return ref pool._components.Buffer[pos];
        }

        public T AddComponent<T>(Entity entity, T component)
        {
            var pool = GetPoolOrCreate<T>();

            int pos = pool.Add(entity, component);

            return pool._components.Buffer[pos];
        }

        public ref T AddComponentRef<T>(Entity entity, T component)
        {
            var pool = GetPoolOrCreate<T>();

            int pos = pool.Add(entity, component);

            return ref pool._components.Buffer[pos];
        }

        #endregion

        public T GetComponent<T>(Entity entity)
        {
            var pool = GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException();

            return pool.Get(entity).Item2;
        }

        public ref T GetComponentRef<T>(Entity entity)
        {
            var pool = GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException();

            return ref pool.GetRef(entity);
        }

#if TODO
        public (T1..., T16) GetComponents<T1..., T16>(int entity)
        {

        }

        public bool HasComponent<T>(int entity)
        {

        }

        public bool HasComponents<T1..., T16>(int entity)
        {

        }

        public View GetView<T1..., T16>()
        {
            
        }

        public T RemoveComponent<T>(int entity)
        {

        }

        public (T1..., T16) RemoveComponents<T1..., T16>(int entity)
        {

        }


        #region Systems

        public void AddSystem(ISystem system)
        {

        }

        public void AddSystemBefore(ISystem system)
        {

        }

        public void AddSystemAfter(ISystem system)
        {

        }

        public void AddSingleFrameSystem(ISystem system)
        {

        }

        public void RemoveSystem(ISystem)
        {

        }

        public void Start()
        {

        }

        public void Update()
        {

        }

        public void End()
        {

        }

        /// <summary>
        /// Executes The [ref="End"] and [ref="Start"] functions
        /// only if already started
        /// </summary>
        public void Restart()
        {

        }

        #endregion


        #region Resources

        public void AddResource(object resource)
        {

        }

        public ref T GetResource<T>()
        {

        }

        #endregion
#endif
        #endregion
    }
}
