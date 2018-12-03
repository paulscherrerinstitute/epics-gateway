using System;
using System.Runtime.Serialization;

namespace GWLogger.Backend.DataContext.Query
{
    [Serializable]
    public class MissingTokenException : Exception
    {
        public MissingTokenException()
        {
        }

        public MissingTokenException(string message) : base(message)
        {
        }

        public MissingTokenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingTokenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}