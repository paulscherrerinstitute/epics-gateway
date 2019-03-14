namespace LoadPerformance
{
    internal enum EpicsCommand : ushort
    {
        EVENT_ADD = 1,
        EVENT_CANCEL = 2,
        SEARCH = 6,
        CREATE_CHANNEL = 18,
        ACCESS_RIGHTS = 22,
        ECHO = 0,
        HOST = 21,
        USER = 20,
        READ_NOTIFY = 15,
        CLEAR_CHANNEL = 12
    }
}