﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GWLogger.Backend.DataContext.Query
{
    [Serializable]
    public class SyntaxException : Exception
    {
        public SyntaxException()
        {
        }

        public SyntaxException(string message) : base(message)
        {
        }

        public SyntaxException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SyntaxException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}