using System;
using System.Runtime.Serialization;

namespace LoadPerformance
{
    internal static class MessageVerifier
    {
        public static void Verify(byte[] data, bool isRequest)
        {
            var main = DataPacket.Create(data, data.Length, true);
            var splitter = new Splitter();
            foreach (var p in splitter.Split(main))
            {
                Verify(p, isRequest);
            }
            if (splitter.HasRemainingData)
            {
                throw new IncompleteMessageException();
            }
        }

        public static void VerifyDataType(ushort dataType)
        {
            if (dataType > 34)
                throw new WrongDataTypeException("Data type " + dataType + " is unknown");
        }

        public static void Verify(DataPacket p, bool isRequest)
        {
            if (p.MessageSize % 8 != 0)
                throw new WrongMessageSizeException("Message size must be a multiple of 8 bytes");

            switch (p.Command)
            {
                case 0: // Version
                    if (p.PayloadSize != 0)
                        throw new WrongMessageSizeException("Version messages must have 0 size");
                    if (p.Parameter1 != 0 || p.Parameter2 != 0)
                        throw new WrongMessageFormatingException("Parameter1 and Parameter2 of Version response must be 0");
                    break;
                case 1: // EventAdd
                    if (p.Parameter1 != 1 && !isRequest)
                        throw new WrongMessageFormatingException("Parameter1 of EventAdd response must be 1");
                    if (isRequest && p.PayloadSize != 16)
                        throw new WrongMessageSizeException("EventAdd messages request must have size 16");
                    VerifyDataType(p.DataType);
                    break;
                case 2: // EventCancel
                    if (p.PayloadSize != 0)
                        throw new WrongMessageSizeException("Version messages must have size 0");
                    VerifyDataType(p.DataType);
                    break;
                case 4: // EventWrite
                    VerifyDataType(p.DataType);
                    break;
                case 6: // Search
                    break;
                case 8: // EventOff
                    if (p.Parameter1 != 0 || p.Parameter2 != 0 || p.PayloadSize != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("Parameter1 and Parameter2 and PayloadSize and DataType and DataCount of EventOff must be 0");
                    break;
                case 9: // EventOn
                    if (p.Parameter1 != 0 || p.Parameter2 != 0 || p.PayloadSize != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("Parameter1 and Parameter2 and PayloadSize and DataType and DataCount of EventOn must be 0");
                    break;
                case 11: // Error
                    break;
                case 12: // ClearChannel
                    if (p.PayloadSize != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("PayloadSize and DataType and DataCount of ClearChannel must be 0");
                    break;
                case 13: // Beacon
                    break;
                case 15: // ReadNotify
                    if (isRequest && p.PayloadSize != 0)
                        throw new WrongMessageFormatingException("PayloadSize of ReadNotify must be 0");
                    VerifyDataType(p.DataType);
                    break;
                case 18: // CreateChannel
                    if (isRequest && (p.DataType != 0 || p.DataCount != 0 || p.PayloadSize == 0))
                        throw new WrongMessageFormatingException("DataType and DataCount of CreateChannel request must be 0");
                    else if (!isRequest)
                    {
                        if (p.PayloadSize != 0)
                            throw new WrongMessageFormatingException("PayloadSizeof CreateChannel answer must be 0");
                        VerifyDataType(p.DataType);
                        if (p.DataCount == 0)
                            throw new WrongMessageFormatingException("DataCount CreateChannel answer must be greater than 0");
                    }
                    break;
                case 19: // WriteNotify
                    VerifyDataType(p.DataType);
                    break;
                case 20: // ClientName
                    if (p.Parameter1 != 0 || p.Parameter2 != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("Parameter1 and Parameter2 and DataType and DataCount of ClientName must be 0");
                    break;
                case 21: // HostName
                    if (p.Parameter1 != 0 || p.Parameter2 != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("Parameter1 and Parameter2 and DataType and DataCount of HostName must be 0");
                    break;
                case 22: // AccessRight
                    if (isRequest)
                        throw new WrongMessageException("AccessRight is only an answer");
                    if (p.PayloadSize != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("PayloadSize and DataType and DataCount of AccessRight must be 0");
                    break;
                case 23: // Echo
                    if (p.Parameter1 != 0 || p.Parameter2 != 0 || p.PayloadSize != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("Parameter1 and Parameter2 and PayloadSize and DataType and DataCount of Echo must be 0");
                    break;
                case 27: // ServerDisconnect
                    if (isRequest)
                        throw new WrongMessageException("ServerDisconnect is only an answer");
                    if (p.Parameter2 != 0 || p.PayloadSize != 0 || p.DataType != 0 || p.DataCount != 0)
                        throw new WrongMessageFormatingException("Parameter2 and PayloadSize and DataType and DataCount of Echo must be 0");
                    break;
                default:
                    throw new WrongMessageCommandException("Command " + p.Command + " is unkown");
            }
        }
    }

    [Serializable]
    internal class WrongMessageCommandException : Exception
    {
        public WrongMessageCommandException()
        {
        }

        public WrongMessageCommandException(string message) : base(message)
        {
        }

        public WrongMessageCommandException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongMessageCommandException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class WrongMessageException : Exception
    {
        public WrongMessageException()
        {
        }

        public WrongMessageException(string message) : base(message)
        {
        }

        public WrongMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class WrongMessageSizeException : Exception
    {
        public WrongMessageSizeException()
        {
        }

        public WrongMessageSizeException(string message) : base(message)
        {
        }

        public WrongMessageSizeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongMessageSizeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class WrongDataTypeException : Exception
    {
        public WrongDataTypeException()
        {
        }

        public WrongDataTypeException(string message) : base(message)
        {
        }

        public WrongDataTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongDataTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class WrongMessageFormatingException : Exception
    {
        public WrongMessageFormatingException()
        {
        }

        public WrongMessageFormatingException(string message) : base(message)
        {
        }

        public WrongMessageFormatingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongMessageFormatingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class IncompleteMessageException : Exception
    {
        public IncompleteMessageException()
        {
        }

        public IncompleteMessageException(string message) : base(message)
        {
        }

        public IncompleteMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IncompleteMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
