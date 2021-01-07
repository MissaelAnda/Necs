using System;
using System.Runtime.CompilerServices;

namespace Necs
{
    public abstract class BaseSystem
    {
        public ViewDescriptor Descriptor = new ViewDescriptor();

        protected Registry Registry { get; private set; }

        public BaseSystem()
        { }

        public BaseSystem(params Type[] types)
        {
            Descriptor.With(types);
        }

        public BaseSystem(Type[] types, Type[] without)
        {
            Descriptor.With(types);
            Descriptor.Without(without);
        }

        public BaseSystem(ViewDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        internal void SetRegistry(Registry registry)
        {
            Registry = registry;
        }

        protected void EnqueueSingleFrame(ISingleFrameSystem system)
        {
            Registry?.EnqueueSingleFrameSystem(system);
        }

        protected void EnqueuePreProcess(IPreProcessSystem system)
        {
            Registry?.EnqueuePreProcessSystem(system);
        }

        protected void EnqueuePostProcess(IPostProcessSystem system)
        {
            Registry?.EnqueuePostProcessSystem(system);
        }

        #region builder

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewDescriptor With<T>() => Descriptor.With(typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewDescriptor With(params Type[] types) => Descriptor.With(types);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewDescriptor Without<T>() => Descriptor.Without(typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewDescriptor Without(params Type[] types) => Descriptor.Without(types);

        #endregion
    }
}
