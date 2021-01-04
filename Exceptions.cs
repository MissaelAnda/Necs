using System;

namespace Necs
{
    public class InvalidEntityException : Exception
    { }

    public class InvalidComponentException : Exception
    {
        public InvalidComponentException(Type component) :
            base($"The component {component.FullName} does not exists in the registry.")
        { }

        protected InvalidComponentException(string message) : base(message)
        { }
    }

    public class MissingComponentException : InvalidComponentException
    {
        public MissingComponentException(Entity entity, Type component) :
            base($"The entity {entity.Id} doesn't have the component {component.FullName}.")
        { }
    }
}
