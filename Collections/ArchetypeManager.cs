using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Necs
{
    internal class ArchetypeManager
    {
        Dictionary<int, Archetype> _archetypes = new Dictionary<int, Archetype>();
        Registry _registry;

        public ArchetypeManager(Registry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// Returns the archetype if exists, else return <see langword="null"/>
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Archetype GetArchetype(params Type[] types)
        {
            _archetypes.TryGetValue(CreateHash(types), out var archetype);
            return archetype;
        }

        /// <summary>
        /// Gets the archetype, if it doesn't exists creates it
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Archetype GetArchetypeOrCreate(params Type[] types)
        {
            var hash = CreateHash(types);
            if (_archetypes.TryGetValue(hash, out var archetype))
                return archetype;

            archetype = new Archetype(hash, _registry, types);

            _archetypes.Add(hash, archetype);

            return archetype;
        }

        public void RemoveArchetypesWith(Type type)
        {
            foreach (var pair in _archetypes)
                if (pair.Value.HasType(type))
                    _archetypes.Remove(pair.Value.Hash);
        }

        public Archetype[] GetArchetypes(Type[] types, Type[] without = null)
        {
            List<Archetype> archetypes = new List<Archetype>(10);
            foreach (var archetype in _archetypes.Values)
            {
                if (archetype.HasTypes(types) &&
                    (without == null || !archetype.HasTypes(without)))
                    archetypes.Add(archetype);
            }

            return archetypes.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CreateHash(params Type[] types)
        {
            var hash = new StringBuilder(types.Length);
            foreach (var type in types)
                hash.Append(type.FullName);
            return hash.ToString().GetHashCode();
        }
    }

    internal class Archetype
    {
        public readonly int Hash;

        public FillableList<Entity> Entities = new FillableList<Entity>(100);

        public int ComponentsCount => ComponentPools.Count;

        public int EntitiesCount => Entities.Count;

        public Dictionary<Type, IComponentPool> ComponentPools;

        public Archetype(Registry registry, params Type[] types) :
            this(ArchetypeManager.CreateHash(types), registry, types)
        { }

        public Archetype(int hash, Registry registry, params Type[] types)
        {
            ComponentPools = new Dictionary<Type, IComponentPool>(types.Length);
            Hash = hash;

            for (int i = 0; i < types.Length; i++)
            {
                var pool = registry.GetPool(types[i]);
                if (pool == null)
                    throw new InvalidComponentException(types[i]);

                ComponentPools.Add(types[i], pool);
            }
        }

        public ComponentPool<T> GetPool<T>()
        {
            if (ComponentPools.TryGetValue(typeof(T), out var pool))
                return pool as ComponentPool<T>;

            return null;
        }

        public void AddEntity(Entity entity)
        {
            Entities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            Entities.Replace(entity, Entity.Invalid);
        }

        public Type[] GetTypesWith(params Type[] with)
        {
            // Assumes no type will be duplicated
            var res = new Type[with.Length + ComponentPools.Keys.Count];
            ComponentPools.Keys.CopyTo(res, 0);
            with.CopyTo(res, ComponentPools.Keys.Count);
            return res;
        }

        public Type[] GetTypesWithout(params Type[] without)
        {
            // Assumes that "without" is a subset of "Types"
            var res = new Type[ComponentPools.Count - without.Length];

            int pos = 0;
            bool found;
            foreach (var key in ComponentPools.Keys)
            {
                found = false;
                for (int j = 0; j < without.Length; j++)
                {
                    if (key == without[j])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    res[pos++] = key;
            }

            return res;
        }

        public bool HasType(Type type)
        {
            return ComponentPools.ContainsKey(type);
        }


        public bool HasTypes(params Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
                if (!HasType(types[i]))
                    return false;

            return true;
        }

        public IComponentPool this[Type type] => ComponentPools[type];
    }
}
