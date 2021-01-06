using System;

namespace Necs
{
    /// <summary>
    /// Thrown when an invalid entity is given to the registry. A invalid entity has an incorrect index and/or version.
    /// </summary>
    public class InvalidEntityException : Exception
    { }

    /// <summary>
    /// Thrown when the registry doesn't have the component registered
    /// </summary>
    public class InvalidComponentException : Exception
    {
        public InvalidComponentException(Type component) :
            base($"The component {component.FullName} does not exists in the registry.")
        { }

        internal InvalidComponentException(string message) : base(message)
        { }
    }

    /// <summary>
    /// Thrown when the entity doesn't have the asked component on removal and get
    /// </summary>
    public class MissingComponentException : InvalidComponentException
    {
        public MissingComponentException(Entity entity, Type component) :
            base($"The entity {entity.Id} doesn't have the component {component.FullName}.")
        { }
    }

    /// <summary>
    /// Thrown when one or more components in a view doesn't exist in the registry
    /// </summary>
    public class InvalidViewException : InvalidComponentException
    {
        internal static InvalidViewException Create(Registry registry, params Type[] types)
        {
            string error = "The registry doesn't have the component(s):\n";
            for (int i = 0; i < types.Length; i++)
                if (!registry.ComponentExists(types[i]))
                    error += types[i].FullName + "\n";
            
            return new InvalidViewException(error);
        }

        internal InvalidViewException(string message) : base(message)
        { }
    }
}
