using System.Runtime.CompilerServices;

namespace Necs
{
    public struct Entity
    {
        public readonly long Id;

        public static readonly Entity Invalid = new Entity(-1, 0);

        public int Version => (int)(Id >> 32);

        public int Index => (int)Id;

        public Entity(int index, int version)
        {
            Id = ((long)version << 32 | (long)index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => IsValidEntity(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidEntity(Entity entity)
        {
            return (entity.Id >> 32) != (int)-1;
        }

        public static bool operator ==(in Entity a, in Entity b) => a.Id == b.Id;

        public static bool operator !=(in Entity a, in Entity b) => a.Id != b.Id;

        public override string ToString()
        {
            return $"Entity [ID: {Id} - Index: {Index} - Version: {Version}]";
        }
    }
}
