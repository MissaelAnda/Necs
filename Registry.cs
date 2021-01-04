using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Necs
{
    public struct Entity
    {
        public readonly long Id;

        public int Version => (int)(Id >> 32);

        public int Index => (int)Id;

        public Entity(int index, int version)
        {
            Id = ((long)version << 32 | (long)index);
        }

        public bool IsValid() => IsValidEntity(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidEntity(Entity entity)
        {
            return (entity.Id >> 32) != (int)-1;
        }

        public static bool operator ==(Entity a, Entity b) => a.Id == b.Id;

        public static bool operator !=(Entity a, Entity b) => a.Id != b.Id;

        public override string ToString()
        {
            return $"Entity [ID: {Id} - Index: {Index} - Version: {Version}]";
        }
    }

    public class Registry
    {
        /// <summary>
        /// The amount of registered components
        /// </summary>
        public int ComponentPoolsCount => _componentPools.Count;
        
        /// <summary>
        /// The amount of valid entities
        /// </summary>
        public int EntitiesCount => _entities.Count;

        internal FillableList<Entity> _entities = new FillableList<Entity>(1024);
        internal List<IComponentPool> _componentPools = new List<IComponentPool>();
        internal Dictionary<Type, int> _componentPoolIndices = new Dictionary<Type, int>();

        bool _started = false;

        public Registry()
        {
            // Prevent deleting the entity so we keep its version when deleted
            _entities.Invalidate = false;
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

        public Entity CreateEntityWith<T1>() where T1 : new()
        {
            var entity = CreateEntity();

            var pool = GetPoolOrCreate<T1>();

            pool.Add(entity, new T1());

            return entity;
        }

        public Entity CreateEntityWith<T1>(T1 component)
        {
            var entity = CreateEntity();

            var pool = GetPoolOrCreate<T1>();

            pool.Add(entity, component);

            return entity;
        }

        public void DeleteEntity(Entity entity)
        {
            ValidateEntity(entity);

            for (int i = 0; i < _componentPools.Count; i++)
                _componentPools[i].Delete(entity);

            _entities.RemoveAt(entity.Index);
        }

        #region AddComponent<T1.., T16>

        /// <summary>
        /// Creates and adds a component to the entity if it doesn't have one already
        /// </summary>
        /// <exception cref="InvalidEntityException">Throws when the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">The entity to add the component</param>
        /// <returns></returns>
        public Registry AddComponent<T>(Entity entity) where T : new()
        {
            ValidateEntity(entity);

            var pool = GetPoolOrCreate<T>();

            pool.Add(entity, new T());

            return this;
        }

        /// <summary>
        /// Adds the given component to the entity if it doesn't have one already
        /// </summary>
        /// <exception cref="InvalidEntityException">Throws when the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">The entity to add the component</param>
        /// <param name="component">The component</param>
        /// <returns></returns>
        public Registry AddComponent<T>(Entity entity, T component)
        {
            ValidateEntity(entity);

            var pool = GetPoolOrCreate<T>();

            pool.Add(entity, component);

            return this;
        }

        /// <summary>
        /// Creates and sets the component to the entity and replace the old one if there is
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">The entity to add the component</param>
        /// <returns></returns>
        public Registry SetComponent<T>(Entity entity) where T : new()
        {
            ValidateEntity(entity);

            var pool = GetPoolOrCreate<T>();

            var pos = pool.GetComponentPos(entity);

            if (pos.HasValue)
                pool._components.Buffer[pos.Value] = new T();
            else
                pool.Add(entity, new T());

            return this;
        }

        /// <summary>
        /// Sets the component to the entity and replace the old one if there is
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">The entity to add the component</param>
        /// <param name="value">The component</param>
        /// <returns></returns>
        public Registry SetComponent<T>(Entity entity, T value)
        {
            ValidateEntity(entity);

            var pool = GetPoolOrCreate<T>();

            var pos = pool.GetComponentPos(entity);

            if (pos.HasValue)
                pool._components.Buffer[pos.Value] = value;
            else
                pool.Add(entity, value);

            return this;
        }

        #endregion

        /// <summary>
        /// Gets the component, if the component is a struct it will return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exists</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have the component</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>The component</returns>
        public T GetComponent<T>(Entity entity)
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException(typeof(T));

            var (has, component) = pool.Get(entity);

            if (!has)
                throw new MissingComponentException(entity, typeof(T));

            return component;
        }

        /// <summary>
        /// Gets a reference the component, used for struct components
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the entity doesn't have the component</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have the component</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>The component</returns>
        public ref T GetComponentRef<T>(Entity entity)
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException(typeof(T));

            return ref pool.GetRef(entity);
        }

        /// <summary>
        /// Gets the component or creates it. For struct components use <see cref="GetComponentOrCreateRef{T}(Entity)"/>
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity"></param>
        /// <returns>The component</returns>
        public T GetComponentOrCreate<T>(Entity entity) where T : new()
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            int pos;
            if (pool == null)
            {
                pool = new ComponentPool<T>();
                pos = pool.Add(entity, new T());
                _componentPoolIndices.Add(typeof(T), _componentPools.Count);
                _componentPools.Add(pool);
            }
            else
            {
                int? has = pool.GetComponentPos(entity);
                if (!has.HasValue)
                    pos = pool.Add(entity, new T());
                else
                    pos = has.Value;
            }

            return pool._components.Buffer[pos];
        }

        /// <summary>
        /// Gets or creates a reference to the entity's component
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">The entity</param>
        /// <returns>A ref to the component</returns>
        public ref T GetComponentOrCreateRef<T>(Entity entity) where T : new()
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            int pos;
            if (pool == null)
            {
                pool = new ComponentPool<T>();
                pos = pool.Add(entity, new T());
                _componentPoolIndices.Add(typeof(T), _componentPools.Count);
                _componentPools.Add(pool);
            }
            else
            {
                int? has = pool.GetComponentPos(entity);
                if (!has.HasValue)
                    pos = pool.Add(entity, new T());
                else
                    pos = has.Value;
            }

            return ref pool._components.Buffer[pos];
        }

        #region GetComponents as tuples

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2) GetComponents<T1, T2>(Entity entity)
        {
            ValidateEntity(entity);

            return (GetComponent<T1>(entity), GetComponent<T2>(entity));
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3) GetComponents<T1, T2, T3>(Entity entity)
        {
            ValidateEntity(entity);

            return (GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity));
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3, T4) GetComponents<T1, T2, T3, T4>(Entity entity)
        {
            ValidateEntity(entity);

            return (
                GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity),
                GetComponent<T4>(entity)
            );
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5) GetComponents<T1, T2, T3, T4, T5>(Entity entity)
        {
            ValidateEntity(entity);

            return (
                GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity),
                GetComponent<T4>(entity), GetComponent<T5>(entity)
            );
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6) GetComponents<T1, T2, T3, T4, T5, T6>(Entity entity)
        {
            ValidateEntity(entity);

            return (
                GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity),
                GetComponent<T4>(entity), GetComponent<T5>(entity), GetComponent<T6>(entity)
            );
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6, T7) GetComponents<T1, T2, T3, T4, T5, T6, T7>(Entity entity)
        {
            ValidateEntity(entity);

            return (
                GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity),
                GetComponent<T4>(entity), GetComponent<T5>(entity), GetComponent<T6>(entity),
                GetComponent<T7>(entity)
            );
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6, T7, T8) GetComponents<T1, T2, T3, T4, T5, T6, T7, T8>(Entity entity)
        {
            ValidateEntity(entity);

            return (
                GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity),
                GetComponent<T4>(entity), GetComponent<T5>(entity), GetComponent<T6>(entity),
                GetComponent<T7>(entity), GetComponent<T8>(entity)
            );
        }

        /// <summary>
        /// Returns a tuple with the components, if one component type is a struct it'll return a copy
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have one of the components</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6, T7, T8, T9) GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Entity entity)
        {
            ValidateEntity(entity);

            return (
                GetComponent<T1>(entity), GetComponent<T2>(entity), GetComponent<T3>(entity),
                GetComponent<T4>(entity), GetComponent<T5>(entity), GetComponent<T6>(entity),
                GetComponent<T7>(entity), GetComponent<T8>(entity), GetComponent<T9>(entity)
            );
        }

        #endregion

        /// <summary>
        /// Checks if the entity has the component
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component to check</typeparam>
        /// <param name="entity">The entity</param>
        /// <returns></returns>
        public bool HasComponent<T>(Entity entity)
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool == null)
                return false;

            return pool.Has(entity);
        }

        /// <summary>
        /// Gets the amount of components the entity has
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int ComponentsCount(Entity entity)
        {
            ValidateEntity(entity);

            int components = 0;
            for (int i = 0; i < _componentPools.Count; i++)
                if (_componentPools[i].Has(entity))
                    components++;

            return components;
        }

        /// <summary>
        /// Checks if the entity doesn't have any components at all
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsEntityEmpty(Entity entity)
        {
            ValidateEntity(entity);

            for (int i = 0; i < _componentPools.Count; i++)
                if (_componentPools[i].Has(entity))
                    return false;

            return true;
        }

        /// <summary>
        /// Checks if the component type exists in the registry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool ComponentExists<T>()
        {
            return GetPool<T>() != null;
        }

        /// <summary>
        /// Removes the component from the entity. To get the removed component <see cref="GetAndRemoveComponent{T}(Entity)"/>
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Registry RemoveComponent<T>(Entity entity)
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool != null)
                pool.Remove(entity);

            return this;
        }

        /// <summary>
        /// Removes all components from the entity
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Registry RemoveAllComponents(Entity entity)
        {
            ValidateEntity(entity);

            for (int i = 0; i < _componentPools.Count; i++)
            {
                _componentPools[i].Delete(entity);
            }

            return this;
        }

        /// <summary>
        /// Removes and returns the component
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <exception cref="InvalidComponentException">If the component doesn't exist in the registry</exception>
        /// <exception cref="MissingComponentException">If the entity doesn't have the component</exception>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public T GetAndRemoveComponent<T>(Entity entity)
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException(typeof(T));

            var (existed, component) = pool.Remove(entity);

            if (!existed)
                throw new MissingComponentException(entity, typeof(T));

            return component;
        }

#if TODO

        public View GetView<T1..., T16>()
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

        /// <summary>
        /// Deletes the component pools that are empty to save space
        /// </summary>
        public void Clean()
        {
            foreach (var pool in _componentPools)
            {
                if (pool.Count == 0)
                    _componentPools.Remove(pool);
            }
        }

        /// <summary>
        /// Clears the component pools unused memory
        /// </summary>
        public void CleanCache()
        {
            // TODO
        }

        /// <summary>
        /// Clears all entities, components and system but keeps the memory ready
        /// </summary>
        public void Clear()
        {
            // TODO
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateEntity(Entity entity)
        {
            if (_entities.Buffer[entity.Index] != entity || !_entities.IsValidIndex(entity.Index))
                throw new InvalidEntityException();
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
    }
}
