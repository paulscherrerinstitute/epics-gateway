using System;
using System.Runtime.Serialization;

namespace GatewayLogic
{
    [Serializable]
    internal class DeadLockException : Exception
    {
        public DeadLockException()
        {
        }

        public DeadLockException(string message) : base(message)
        {
        }

        public DeadLockException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeadLockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}