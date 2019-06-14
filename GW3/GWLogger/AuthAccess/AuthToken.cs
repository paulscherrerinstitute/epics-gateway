using System;

namespace GWLogger.AuthAccess
{
    public class AuthToken
    {
        public bool IsExpired => ExpiresOn.CompareTo(DateTime.Now) <= 0;
        public string RemoteAddress { get; internal set; }
        public string Id { get; internal set; }
        public string Login { get; internal set; }
        public string Email { get; internal set; }
        public DateTime CreatedOn { get; internal set; }
        public DateTime ExpiresOn { get; internal set; }
    }
}