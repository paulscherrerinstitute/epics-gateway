using System;
using System.Runtime.Serialization;

namespace GWLogger.Backend.DataContext.Query
{
    [Serializable]
    public class UnknownConditionException : Exception
    {
        public UnknownConditionException()
        {
        }

        public UnknownConditionException(string message) : base(message)
        {
        }

        public UnknownConditionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownConditionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}