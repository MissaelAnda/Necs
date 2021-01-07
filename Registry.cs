using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Necs
{
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

        /// <summary>
        /// Whether the system execution has begun
        /// </summary>
        public bool Started { get; private set; } = false;

        /// <summary>
        /// Whether is currently executing start systems
        /// </summary>
        public bool Starting { get; private set; } = false;

        /// <summary>
        /// Whether is currently executing process systems
        /// </summary>
        public bool Processing { get; private set; } = false;

        /// <summary>
        /// Whether is currently executing end systems
        /// </summary>
        public bool Ending { get; private set; } = false;

        FillableList<Entity> _entities = new FillableList<Entity>(1024);
        List<IComponentPool> _componentPools = new List<IComponentPool>();
        Dictionary<Type, int> _componentPoolIndices = new Dictionary<Type, int>();
        SparsedList<Archetype> _entityArchetype = new SparsedList<Archetype>(1024);
        ArchetypeManager _archetypeManager;

        #region Systems

        List<IStartSystem> _startSystems = new List<IStartSystem>();

        List<IProcessSystem> _processSystems = new List<IProcessSystem>();

        List<IEndSystem> _endSystems = new List<IEndSystem>();

        Queue<IPreProcessSystem> _preProcessSystems = new Queue<IPreProcessSystem>();

        Queue<IPostProcessSystem> _postProcessSystems = new Queue<IPostProcessSystem>();

        Queue<BaseSystem> _queuedSystems = new Queue<BaseSystem>();

        Queue<ISingleFrameSystem> _singleFrameSystems = new Queue<ISingleFrameSystem>();

        #region Notificables

        Action<Registry> _startNotificables;

        Action<Registry> _endNotificables;

        #endregion

        bool _restart = false;

        #endregion

        public Registry()
        {
            // Prevent deleting the entity so we keep its version when deleted
            _entities.Invalidate = false;
            _archetypeManager = new ArchetypeManager(this);
        }

        #region API

        #region Entities

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

            // Create the entity-archetype relationship
            AddEntityTypes(entity, typeof(T1));

            return entity;
        }

        public Entity CreateEntityWith<T1>(T1 component)
        {
            var entity = CreateEntity();

            var pool = GetPoolOrCreate<T1>();

            pool.Add(entity, component);

            // Create the entity-archetype relationship
            AddEntityTypes(entity, typeof(T1));

            return entity;
        }

        public void DeleteEntity(Entity entity)
        {
            ValidateEntity(entity);

            // Get and remove the entity-archetype relationship
            var archetype = _entityArchetype[entity.Index];
            // The archetype can be null if the entity didn't have any components attached
            archetype?.RemoveEntity(entity);
            _entityArchetype[entity.Index] = null;

            // Only iterate through the component pools we know have the entity
            if (archetype != null)
                for (int i = 0; i < archetype.ComponentPools.Length; i++)
                    archetype.ComponentPools[i].Delete(entity);

            _entities.Remove(entity);
        }

        #endregion

        #region Components

        /// <summary>
        /// Creates the component pool if it didn't exist. Helps to prevent <see cref="InvalidComponentException"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RegisterComponent<T>()
        {
            GetPoolOrCreate<T>();
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

            if (pool.Has(entity))
                return this;

            pool.Add(entity, new T());

            AddEntityTypes(entity, typeof(T));

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

            if (pool.Has(entity))
                return this;

            pool.Add(entity, component);

            AddEntityTypes(entity, typeof(T));

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
            {
                pool.Add(entity, new T());
                AddEntityTypes(entity, typeof(T));
            }

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
            {
                pool.Add(entity, value);
                AddEntityTypes(entity, typeof(T));
            }

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
        /// Gets the component, if it doesn't exists it'll return null instead
        /// of throwing <see cref="MissingComponentException"/> or <see cref="InvalidComponentException"/>
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>The component</returns>
        public T GetComponentOrNull<T>(Entity entity) where T : class
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool == null)
                return null;

            var (has, component) = pool.Get(entity);

            if (!has)
                return null;

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

            if (!pool.Has(entity))
                throw new MissingComponentException(entity, typeof(T));

            return ref pool.GetRef(entity);
        }

        /// <summary>
        /// Gets the component, if it doesn't exists it'll return null instead
        /// of throwing <see cref="MissingComponentException"/> or <see cref="InvalidComponentException"/>
        /// </summary>
        /// <exception cref="InvalidEntityException">If the entity is not valid</exception>
        /// <typeparam name="T">The component type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>The component</returns>
        public T? GetComponentOrNullStruct<T>(Entity entity) where T : struct
        {
            ValidateEntity(entity);

            var pool = GetPool<T>();

            if (pool == null)
                return null;

            var (has, component) = pool.Get(entity);

            if (!has)
                return null;

            return component;
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

            var pool = GetPoolOrCreate<T>();

            int pos;
            int? has = pool.GetComponentPos(entity);

            // Add the component
            if (!has.HasValue)
            {
                pos = pool.Add(entity, new T());

                AddEntityTypes(entity, typeof(T));
            }
            // Component already exists
            else
                pos = has.Value;

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

            var pool = GetPoolOrCreate<T>();

            int pos;
            int? has = pool.GetComponentPos(entity);

            // Add the component
            if (!has.HasValue)
            {
                pos = pool.Add(entity, new T());

                AddEntityTypes(entity, typeof(T));
            }
            // Component already exists
            else
                pos = has.Value;

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

            var archetype = _entityArchetype[entity.Index];

            if (archetype == null)
                return false;

            return archetype.HasType(typeof(T));
        }

        /// <summary>
        /// Gets the amount of components the entity has
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int ComponentsCount(Entity entity)
        {
            ValidateEntity(entity);

            var archetype = _entityArchetype[entity.Index];

            if (archetype == null)
                return 0;

            return archetype.ComponentsCount;
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

            return _entityArchetype[entity.Index] == null;
        }

        /// <summary>
        /// Checks if the component type exists in the registry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ComponentExists<T>()
        {
            return GetPool<T>() != null;
        }

        /// <summary>
        /// Checks if the component type exists in the registry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ComponentExists(Type type)
        {
            return GetPool(type) != null;
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

            if (pool == null || pool.Has(entity))
                return this;

            pool.Remove(entity);

            RemoveEntityTypes(entity, typeof(T));

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

            var pool = _entityArchetype[entity.Index];

            if (pool == null)
                return this;

            for (int i = 0; i < pool.ComponentsCount; i++)
                pool.ComponentPools[i].Delete(entity);

            pool.RemoveEntity(entity);
            _entityArchetype[entity.Index] = null;

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

            RemoveEntityTypes(entity, typeof(T));

            return component;
        }

        #endregion

        /// <summary>
        /// Creates a view with the components
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public View GetView(params Type[] types)
        {
            return new View(_archetypeManager.GetArchetypes(types));
        }

        /// <summary>
        /// Creates a view from a descriptor
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public View GetView(ViewDescriptor descriptor)
        {
            return new View(_archetypeManager.GetArchetypes(
                descriptor.WithComponents.ToArray(), descriptor.WithoutComponents.Count > 0 ?
                    descriptor.WithoutComponents.ToArray() : null)
            );
        }

        #region Systems

        /// <summary>
        /// Adds the start, process and end systems from the system, doesn't check if already exists
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public Registry AddSystem(BaseSystem system)
        {
            if (system is IStartSystem start)
                _startSystems.Add(start);
            if (system is IProcessSystem process)
                _processSystems.Add(process);
            if (system is IEndSystem end)
                _endSystems.Add(end);

            return this;
        }

        /// <summary>
        /// Creates and adds the system, checks if already exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>the system instance</returns>
        public T AddSystem<T>() where T : BaseSystem, new()
        {
            var system = GetSystem<T>();
            if (system != null)
                return system;

            system = new T();
            AddSystem(system);

            return system;
        }

        /// <summary>
        /// Adds the system before the first system of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="system"></param>
        public void AddSystemBefore<T>(BaseSystem system) where T : BaseSystem
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the system after the first system of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="system"></param>
        public void AddSystemAfter<T>(BaseSystem system) where T : BaseSystem
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the system before the first system of type <paramref name="type"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="system"></param>
        public void AddSystemBefore(Type type, BaseSystem system)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the system after the first system of type <paramref name="type"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="system"></param>
        public void AddSystemAfter(Type type, BaseSystem system)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds an action when registry lifecycle starts
        /// </summary>
        /// <param name="notificable"></param>
        public void AddStartNotificable(Action<Registry> notificable)
        {
            _startNotificables += notificable;
        }

        /// <summary>
        /// Adds an action when registry lifecycle ends
        /// </summary>
        /// <param name="notificable"></param>
        public void AddEndNotificable(Action<Registry> notificable)
        {
            _endNotificables += notificable;
        }

        /// <summary>
        /// Removes an action from starts
        /// </summary>
        /// <param name="notificable"></param>
        public void RemoveStartNotificable(Action<Registry> notificable)
        {
            _startNotificables -= notificable;
        }

        /// <summary>
        /// Removes an action from end
        /// </summary>
        /// <param name="notificable"></param>
        public void RemoveEndNotificable(Action<Registry> notificable)
        {
            _endNotificables -= notificable;
        }

        /// <summary>
        /// Removes a systems from the registry
        /// </summary>
        /// <param name="system"></param>
        public void RemoveSystem(BaseSystem system)
        {
            if (system is IStartSystem start)
                _startSystems.Remove(start);
            if (system is IProcessSystem process)
                _processSystems.Remove(process);
            if (system is IEndSystem end)
                _endSystems.Remove(end);
        }

        /// <summary>
        /// Removes the systems of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveSystem<T>() where T : BaseSystem
        {
            int i = 0;
            foreach (var system in _startSystems)
            {
                if (system is T)
                    _startSystems.RemoveAt(i);
                i++;
            }

            i = 0;
            foreach (var system in _processSystems)
            {
                if (system is T)
                    _startSystems.RemoveAt(i);
                i++;
            }

            i = 0;
            foreach (var system in _endSystems)
            {
                if (system is T)
                    _startSystems.RemoveAt(i);
                i++;
            }
        }

        /// <summary>
        /// Enqueues a single frame system to be executed instantanusly
        /// </summary>
        /// <param name="system"></param>
        public void EnqueueSingleFrameSystem(ISingleFrameSystem system)
        {
            _singleFrameSystems.Enqueue(system);
        }

        public void EnqueuePreProcessSystem(IPreProcessSystem system)
        {
            _preProcessSystems.Enqueue(system);
        }

        public void EnqueuePostProcessSystem(IPostProcessSystem system)
        {
            _postProcessSystems.Enqueue(system);
        }

        /// <summary>
        /// Gets the system if exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSystem<T>() where T : BaseSystem
        {
            for (int i = 0; i < _startSystems.Count; i++)
                if (_startSystems[i] is T)
                    return _startSystems[i] as T;

            for (int i = 0; i < _processSystems.Count; i++)
                if (_processSystems[i] is T)
                    return _processSystems[i] as T;

            for (int i = 0; i < _endSystems.Count; i++)
                if (_endSystems[i] is T)
                    return _endSystems[i] as T;

            return null;
        }

        /// <summary>
        /// Checks if the registry has a system of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasSystem<T>() where T : BaseSystem
        {
            if (HasStartSystem<T>())
                return true;

            if (HasProcessSystem<T>())
                return true;

            if (HasEndSystem<T>())
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the registry has a start system of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasStartSystem<T>() where T : BaseSystem
        {
            for (int i = 0; i < _startSystems.Count; i++)
                if (_startSystems[i] is T)
                    return true;

            return false;
        }

        /// <summary>
        /// Checks if the registry has a process system of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasProcessSystem<T>() where T : BaseSystem
        {
            for (int i = 0; i < _processSystems.Count; i++)
                if (_processSystems[i] is T)
                    return true;

            return false;
        }

        /// <summary>
        /// Checks if the registry has a end system of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasEndSystem<T>() where T : BaseSystem
        {
            for (int i = 0; i < _endSystems.Count; i++)
                if (_endSystems[i] is T)
                    return true;

            return false;
        }

        #region Lifecycle

        /// <summary>
        /// Starts the system's lifecycle
        /// </summary>
        public void Start()
        {
            if (Started)
                return;

            Starting = true;

            _startNotificables.Invoke(this);

            BaseSystem sys;
            foreach (var system in _startSystems)
            {
                sys = (BaseSystem)system;
                sys.SetRegistry(this);
                DispatchSystem(sys.Descriptor, system.Start);
                sys.SetRegistry(null);
                DispatchSingleFrames();
            }

            Starting = false;
            Started = true;
        }

        /// <summary>
        /// Executes the pre, post and process
        /// </summary>
        public void Process()
        {
            if (!Started)
                return;

            Processing = true;

            BaseSystem sys;

            // Dispatch all preprocessors
            IPreProcessSystem pre;
            while (_preProcessSystems.Count > 0)
            {
                pre = _preProcessSystems.Dequeue();
                sys = (BaseSystem)pre;
                sys.SetRegistry(this);
                DispatchSystem(sys.Descriptor, pre.PreProcess);
                sys.SetRegistry(null);
                DispatchSingleFrames();
            }

            // Dispatch processors
            foreach (var system in _processSystems)
            {
                sys = (BaseSystem)system;
                sys.SetRegistry(this);
                DispatchSystem(sys.Descriptor, system.Process);
                sys.SetRegistry(null);
                DispatchSingleFrames();
            }

            // Dispatch postprocessors
            IPostProcessSystem post;
            while (_postProcessSystems.Count > 0)
            {
                post = _postProcessSystems.Dequeue();
                sys = (BaseSystem)post;
                sys.SetRegistry(this);
                DispatchSystem(sys.Descriptor, post.PostProcess);
                sys.SetRegistry(null);
                DispatchSingleFrames();
            }

            Processing = false;

            if (_restart)
            {
                _restart = false;
                End();
                Start();
            }
        }

        /// <summary>
        /// Ends the system's lifecycle
        /// </summary>
        public void End()
        {
            if (!Started)
                return;

            Ending = true;

            BaseSystem sys;
            foreach (var system in _endSystems)
            {
                sys = (BaseSystem)system;
                sys.SetRegistry(this);
                DispatchSystem(sys.Descriptor, system.End);
                sys.SetRegistry(null);
                DispatchSingleFrames();
            }

            _endNotificables.Invoke(this);

            Ending = false;
            Started = false;

            if (_restart)
            {
                _restart = false;
                Start();
            }
        }

        /// <summary>
        /// Restarts the system's lifecycle
        /// </summary>
        public void Restart()
        {
            if (Started)
            {
                if (!Processing && !Starting && !Ending)
                {
                    End();
                    Start();
                }
                else _restart = true;
            }
        }


        /// <summary>
        /// Dispatch automatically a system from an action
        /// </summary>
        /// <param name="descriptor">Describes the components to be included and ignored</param>
        /// <param name="system"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DispatchSystem(ViewDescriptor descriptor, Action<Group> system)
        {
            var view = GetView(descriptor);

            view.Each(system);
        }

        /// <summary>
        /// Internal use to dispatch all single frame systems after every other system
        /// </summary>
        void DispatchSingleFrames()
        {
            ISingleFrameSystem system;
            BaseSystem sys;
            while (_singleFrameSystems.Count > 0)
            {
                system = _singleFrameSystems.Dequeue();
                sys = (BaseSystem)system;
                sys.SetRegistry(this);
                DispatchSystem(sys.Descriptor, system.SingleFrame);
                sys.SetRegistry(null);
            }
        }

        #endregion

        #endregion

        #endregion

        /// <summary>
        /// Deletes the component pools that are empty to save space
        /// </summary>
        public void Clean()
        {
            foreach (var pool in _componentPools)
            {
                if (pool.Count == 0)
                {
                    _componentPools.Remove(pool);
                    _archetypeManager.RemoveArchetypesWith(pool.Type);
                }
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

        void AddEntityTypes(Entity entity, params Type[] types)
        {
            var archetype = _entityArchetype[entity.Index];
            if (archetype == null)
                archetype = _archetypeManager.GetArchetypeOrCreate(types);
            else
            {
                archetype.RemoveEntity(entity);
                archetype = _archetypeManager.GetArchetypeOrCreate(archetype.GetTypesWith(types));
            }

            archetype.AddEntity(entity);
            _entityArchetype[entity.Index] = archetype;
        }

        void RemoveEntityTypes(Entity entity, params Type[] types)
        {
            var archetype = _entityArchetype[entity.Index];

            if (archetype != null)
            {
                archetype.RemoveEntity(entity);
                var newArchetype = archetype.GetTypesWithout(types);
                if (newArchetype.Length == 0)
                {
                    _entityArchetype[entity.Index] = null;
                    return;
                }
                archetype = _archetypeManager.GetArchetypeOrCreate(newArchetype);

                archetype.AddEntity(entity);
                _entityArchetype[entity.Index] = archetype;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ValidateEntity(Entity entity)
        {
            if (_entities.Buffer[entity.Index] != entity || !_entities.IsValidIndex(entity.Index))
                throw new InvalidEntityException();
        }

        internal ComponentPool<T> GetPoolOrCreate<T>()
        {
            var pool = GetPool<T>();
            if (pool != null)
                return pool;

            var type = typeof(T);
            pool = new ComponentPool<T>();
            _componentPoolIndices.Add(type, _componentPools.Count);
            _componentPools.Add(pool);
            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentPool<T> GetPool<T>()
        {
            var type = typeof(T);
            if (_componentPoolIndices.TryGetValue(type, out var index))
                return (ComponentPool<T>)_componentPools[index];

            return null;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IComponentPool GetPool(Type type)
        {
            if (_componentPoolIndices.TryGetValue(type, out var index))
                return _componentPools[index];

            return null;
        }
    }
}
