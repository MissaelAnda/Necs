using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Necs
{
    internal class GroupManager
    {
        Dictionary<int, Group> _groups = new Dictionary<int, Group>();
        Registry _registry;

        public GroupManager(Registry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// Returns the group if exists, else return <see langword="null"/>
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Group GetGroup(params Type[] types)
        {
            _groups.TryGetValue(CreateHash(types), out var group);
            return group;
        }

        /// <summary>
        /// Gets the group, if it doesn't exists creates it
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Group GetGroupOrCreate(params Type[] types)
        {
            var hash = CreateHash(types);
            if (_groups.TryGetValue(hash, out var group))
                return group;

            group = new Group(hash, _registry, types);

            _groups.Add(hash, group);

            return group;
        }

        public void RemoveGroupsWith(Type type)
        {
            foreach (var pair in _groups)
                if (pair.Value.HasType(type))
                    _groups.Remove(pair.Value.Hash);
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

    internal class Group
    {
        public readonly int Hash;
        public readonly Type[] Types;
        public readonly IComponentPool[] ComponentPools;

        public FillableList<Entity> Entities = new FillableList<Entity>(100);

        public int ComponentsCount => Types.Length;

        public int EntitiesCount => Entities.Count;

        public Group(Registry registry, params Type[] types) :
            this(GroupManager.CreateHash(types), registry, types)
        { }

        public Group(int hash, Registry registry, params Type[] types)
        {
            Types = types;
            ComponentPools = new IComponentPool[types.Length];
            Hash = hash;

            for (int i = 0; i < types.Length; i++)
            {
                if ((ComponentPools[i] = registry.GetPool(types[i])) == null)
                    throw new InvalidComponentException(types[i]);
            }
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
            var res = new Type[with.Length + Types.Length];
            Types.CopyTo(res, 0);
            with.CopyTo(res, Types.Length);
            return res;
        }

        public Type[] GetTypesWithout(params Type[] without)
        {
            // Assumes that "without" is a subset of "Types"
            var res = new Type[Types.Length - without.Length];

            int pos = 0;
            bool found;
            for (int i = 0; i < Types.Length; i++)
            {
                found = false;
                for (int j = 0; j < without.Length; j++)
                {
                    if (Types[i] == without[j])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    res[pos++] = Types[i];
            }

            return res;
        }

        public bool HasType(Type type)
        {
            for (int i = 0; i < Types.Length; i++)
                if (Types[i] == type)
                    return true;

            return false;
        }
    }
}
