using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Necs
{
    public class Group
    {
        /// <summary>
        /// The current entity
        /// </summary>
        public Entity Entity { get; internal set; }
        
        /// <summary>
        /// Index of the current iteration
        /// </summary>
        public int Iteration { get; internal set; }

        /// <summary>
        /// Whether is the first iteration
        /// </summary>
        public bool IsFirst => Iteration == 0;

        /// <summary>
        /// Whether is the last iteration
        /// </summary>
        public bool IsLast
        {
            get
            {
                if (!_isLast.HasValue)
                    _isLast = !_view.HasNext(_archetypeIndex, _index);

                return _isLast.Value;
            }
        }

        internal Archetype _archetype;
        internal int _archetypeIndex, _index;

        Dictionary<Type, object> _cache = new Dictionary<Type, object>(16);
        View _view;
        bool? _isLast;

        internal Group(View view)
        {
            _view = view;
        }

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
            _isLast = null;
        }
    }

    public class ViewDescriptor
    {
        public List<Type> WithComponents = new List<Type>(10);
        public List<Type> WithoutComponents = new List<Type>(10);

        public ViewDescriptor()
        { }

        public ViewDescriptor(params Type[] types)
        {
            WithComponents.AddRange(types);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewDescriptor With<T>() => With(typeof(T));


        public ViewDescriptor With(params Type[] types)
        {
            WithComponents.AddRange(types);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewDescriptor Without<T>() => Without(typeof(T));


        public ViewDescriptor Without(params Type[] types)
        {
            WithoutComponents.AddRange(types);
            return this;
        }

        public View Build(Registry registry)
        {
            return registry.GetView(this);
        }
    }

    public class View
    {
        /// <summary>
        /// The amount of entities the view has
        /// </summary>
        public int EntitiesCount
        {
            get
            {
                var count = 0;
                for (int i = 0; i < _archetypes.Length; i++)
                    count += _archetypes[i].EntitiesCount;

                return count;
            }
        }

        Archetype[] _archetypes;

        internal View(Archetype[] archetypes)
        {
            _archetypes = archetypes;
        }

        internal bool HasNext(int archetype, int index)
        {
            int i = index + 1;
            for (; archetype < _archetypes.Length; archetype++)
            {
                for (; i < _archetypes[archetype].Entities.Size; i++)
                {
                    if (_archetypes[archetype].Entities.Buffer[i].IsValid())
                        return true;
                }
                i = 0;
            }

            return false;
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
        /// Iterates through the view entities with enumeration
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(int, Entity)> EnumeratedEntities()
        {
            int i, j, index = 0;

            for (j = 0; j < _archetypes.Length; j++)
                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                    {
                        yield return (index, _archetypes[j].Entities.Buffer[i]);
                        index++;
                    }
        }

        /// <summary>
        /// Executes an action with the view entities with enumeration
        /// </summary>
        /// <param name="action"></param>
        public void Entities(Action<int, Entity> action)
        {
            int i, j, index = 0;

            for (j = 0; j < _archetypes.Length; j++)
                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                    {
                        action.Invoke(index, _archetypes[j].Entities.Buffer[i]);
                        index++;
                    }
        }

        /// <summary>
        /// Iterates through the view giving a entity <see cref="Group"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Group> Each()
        {
            int i, j, iteration = 0;
            var group = new Group(this);

            for (j = 0; j < _archetypes.Length; j++)
            {
                group._archetype = _archetypes[j];
                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                {
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                    {
                        group.Clear();
                        group.Entity = _archetypes[j].Entities.Buffer[i];
                        group.Iteration = iteration;
                        group._archetypeIndex = j;
                        group._index = i;
                        yield return group;
                        iteration++;
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
            int i, j, iteration = 0;
            var group = new Group(this);

            for (j = 0; j < _archetypes.Length; j++)
            {
                group._archetype = _archetypes[j];

                for (i = 0; i < _archetypes[j].Entities.Size; i++)
                {
                    if (_archetypes[j].Entities.Buffer[i].IsValid())
                    {
                        group.Clear();
                        group.Entity = _archetypes[j].Entities.Buffer[i];
                        group.Iteration = iteration;
                        group._archetypeIndex = j;
                        group._index = i;
                        action.Invoke(group);
                        iteration++;
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
