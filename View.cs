using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Necs
{
    public class Group
    {
        public Entity Entity { get; internal set; }
        internal Archetype _archetype;
        Dictionary<Type, object> _cache = new Dictionary<Type, object>(16);

        /// <summary>
        /// Gets the entity's component. For struct components use <see cref="GetRef{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            var type = typeof(T);
            if (_cache.TryGetValue(type, out var cached))
                return (T)cached;

            var pool = _archetype.GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException(typeof(T));

            var component = pool.Get(Entity).Item2;
            _cache.Add(type, component);

            return component;
        }


        /// <summary>
        /// Gets a reference to the entity's component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ref T GetRef<T>()
        {
            var pool = _archetype.GetPool<T>();

            if (pool == null)
                throw new InvalidComponentException(typeof(T));

            return ref pool.GetRef(Entity);
        }

        /// <summary>
        /// Checks if the entity has the component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() => Has(typeof(T));

        /// <summary>
        /// Checks if the entity has the component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Type type) => _archetype.HasType(type);

        #region Unpacking

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2) Unpack<T1, T2>()
        {
            return (Get<T1>(), Get<T2>());
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3) Unpack<T1, T2, T3>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>()
            );
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3, T4) Unpack<T1, T2, T3, T4>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>(),
                Get<T4>()
            );
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5) Unpack<T1, T2, T3, T4, T5>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>(),
                Get<T4>(), Get<T5>()
            );
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6) Unpack<T1, T2, T3, T4, T5, T6>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>(),
                Get<T4>(), Get<T5>(), Get<T6>()
            );
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6, T7) Unpack<T1, T2, T3, T4, T5, T6, T7>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>(),
                Get<T4>(), Get<T5>(), Get<T6>(),
                Get<T7>()
            );
        }
        
        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6, T7, T8) Unpack<T1, T2, T3, T4, T5, T6, T7, T8>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>(),
                Get<T4>(), Get<T5>(), Get<T6>(),
                Get<T7>(), Get<T8>()
            );
        }
        
        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public (T1, T2, T3, T4, T5, T6, T7, T8, T9) Unpack<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        {
            return (
                Get<T1>(), Get<T2>(), Get<T3>(),
                Get<T4>(), Get<T5>(), Get<T6>(),
                Get<T7>(), Get<T8>(), Get<T9>()
            );
        }

        #endregion

        internal void Clear()
        {
            _cache.Clear();
        }
    }

    public class ViewBuilder
    {
        ArchetypeManager _manager;

        List<Type> _with = new List<Type>(10);
        List<Type> _without = new List<Type>(10);

        internal ViewBuilder(ArchetypeManager manager)
        {
            _manager = manager;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewBuilder With<T>() => With(typeof(T));


        public ViewBuilder With(params Type[] types)
        {
            _with.AddRange(types);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewBuilder Without<T>() => Without(typeof(T));


        public ViewBuilder Without(params Type[] types)
        {
            _without.AddRange(types);
            return this;
        }

        public View Build()
        {
            return new View(_manager.GetArchetypes(_with.ToArray(), _without.Count > 0 ? _without.ToArray() : null));
        }
    }

    public class View
    {
        Archetype[] _archetypes;

        int _entitiesCount = -1;

        internal View(Archetype[] archetypes)
        {
            _archetypes = archetypes;
        }

        /// <summary>
        /// The amount of entities the view has
        /// </summary>
        public int EntitiesCount
        {
            get
            {
                if (_entitiesCount == -1)
                {
                    _entitiesCount = 0;
                    for (int i = 0; i < _archetypes.Length; i++)
                        _entitiesCount += _archetypes[i].EntitiesCount;
                }
                return _entitiesCount;
            }
        }

        /// <summary>
        /// Iterates through the view entities
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Entity> Entities()
        {
            int i, j;

            for (j = 0; j < _archetypes.Length; j++)
                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                        yield return _archetypes[j].Entities.Buffer[i];
        }

        /// <summary>
        /// Executes an action with the view entities
        /// </summary>
        /// <param name="action"></param>
        public void Entities(Action<Entity> action)
        {
            int i, j;

            for (j = 0; j < _archetypes.Length; j++)
                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                        action.Invoke(_archetypes[j].Entities.Buffer[i]);
        }

        /// <summary>
        /// Iterates through the view giving a entity <see cref="Group"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Group> Each()
        {
            int i, j;
            var group = new Group();

            for (j = 0; j < _archetypes.Length; j++)
            {
                group._archetype = _archetypes[j];
                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                {
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                    {
                        group.Clear();
                        group.Entity = _archetypes[j].Entities.Buffer[i];
                        yield return group;
                    }
                }
            }
        }

        /// <summary>
        /// Excecutes an action giving a entity <see cref="Group"/>
        /// </summary>
        /// <param name="action"></param>
        public void Each(Action<Group> action)
        {
            int i, j;
            var group = new Group();

            for (j = 0; j < _archetypes.Length; j++)
            {
                group._archetype = _archetypes[j];

                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                {
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                    {
                        group.Clear();
                        group.Entity = _archetypes[j].Entities.Buffer[i];
                        action.Invoke(group);
                    }
                }
            }
        }


        #region Unpacking

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T1> Unpack<T1>()
        {
            foreach (var group in Each())
                yield return group.Get<T1>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2)> Unpack<T1, T2>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3)> Unpack<T1, T2, T3>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3, T4)> Unpack<T1, T2, T3, T4>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3, T4>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3, T4, T5)> Unpack<T1, T2, T3, T4, T5>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3, T4, T5>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3, T4, T5, T6)> Unpack<T1, T2, T3, T4, T5, T6>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3, T4, T5, T6>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3, T4, T5, T6, T7)> Unpack<T1, T2, T3, T4, T5, T6, T7>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3, T4, T5, T6 ,T7>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3, T4, T5, T6, T7, T8)> Unpack<T1, T2, T3, T4, T5, T6, T7, T8>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3, T4, T5, T6, T7, T8>();
        }

        /// <summary>
        /// Iterates through the components unpacking them into tuples
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> Unpack<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        {
            foreach (var group in Each())
                yield return group.Unpack<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
        }

        #endregion

    }
}
