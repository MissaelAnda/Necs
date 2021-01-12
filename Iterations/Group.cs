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
}
