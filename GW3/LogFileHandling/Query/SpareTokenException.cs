using System;
using System.Runtime.Serialization;

namespace GWLogger.Backend.DataContext.Query
{
    [Serializable]
    public class SpareTokenException : Exception
    {
        public SpareTokenException()
        {
        }

        public SpareTokenException(string message) : base(message)
        {
        }

        public SpareTokenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SpareTokenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}