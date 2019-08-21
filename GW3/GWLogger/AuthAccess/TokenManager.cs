using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace GWLogger.AuthAccess
{
    public static class TokenManager
    {
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);

        private static Dictionary<string, AuthToken> tokens = new Dictionary<string, AuthToken>();

        private static SemaphoreSlim locker = new SemaphoreSlim(1);

        static TokenManager()
        {
            AppDomain.CurrentDomain.DomainUnload += (sender, e) => { Dispose(); };
        }

        /// <summary>
        /// The number of tokens registered in the manager.
        /// </summary>
        public static int Count
        {
            get
            {
                locker.Wait();
                try
                {
                    return tokens.Count;
                }
                finally
                {
                    locker.Release();
                }
            }
        }

        /// <summary>
        /// Check, if the token is valid for use from this remote address.
        /// </summary>
        /// <exception cref="InvalidAuthTokenException">Thrown if there is no token with this ID, if the token is expired or if the remote address does not match.</exception>
        /// <param name="tokenId">The token ID</param>
        /// <param name="remoteAddress">The remote IP / network address</param>
        public static void ValidateToken(string tokenId, string remoteAddress)
        {
            remoteAddress = (remoteAddress == "::1" ? "127.0.0.1" : remoteAddress);
            var token = GetToken(tokenId, remoteAddress);
            if (token == null || token.IsExpired)
                throw new Exception("Invalid Token");
            if (token.RemoteAddress != remoteAddress)
                throw new Exception("Invalid Token");
        }

        /// <summary>
        /// Create a new <see cref="AuthToken"/> for a user and register it in the manager.
        /// </summary>
        /// <param name="login">The user's login name</param>
        /// <param name="userId">The user's ID</param>
        /// <param name="firstName">The user's given name</param>
        /// <param name="lastName">The user's surname</param>
        /// <param name="email">The user's email address</param>
        /// <param name="remoteAddress">The remote IP / network address</param>
        /// <returns>The token</returns>
        public static AuthToken CreateToken(string login, string password, string email, string remoteAddress)
        {
            remoteAddress = (remoteAddress == "::1" ? "127.0.0.1" : remoteAddress);
            var token = new AuthToken
            {
                Id = System.Guid.NewGuid().ToString(),
                Login = login,
                Password = password,
                Email = email,
                RemoteAddress = remoteAddress,
                CreatedOn = DateTime.Now,
                ExpiresOn = DateTime.Now.Add(DefaultLifetime)
            };
            locker.Wait();
            try
            {
                tokens.Add(token.Id, token);
            }
            finally
            {
                locker.Release();
            }
            return token;
        }

        /// <summary>
        /// Refresh (extend lifetime) of a token.
        /// </summary>
        /// <param name="tokenId">The token's ID</param>
        public static void RenewToken(string tokenId, string remoteAddress)
        {
            var token = GetToken(tokenId, remoteAddress);
            if (token.IsExpired)
                throw new Exception("Invalid token");
            token.ExpiresOn = DateTime.Now.Add(DefaultLifetime);
        }

        /// <summary>
        /// Remove a token from the manager.
        /// </summary>
        /// <param name="tokenId">The token's ID</param>
        public static void DestroyToken(string tokenId)
        {
            locker.Wait();
            try
            {
                if (tokens.ContainsKey(tokenId))
                    tokens.Remove(tokenId);
                else
                    throw new Exception("Invalid token");
            }
            finally
            {
                locker.Release();
            }
        }

        /// <summary>
        /// Obtain a token from the manager
        /// </summary>
        /// <param name="tokenId"></param>
        /// <returns>The <see cref="AuthToken"/></returns>
        /// <exception cref="IvException">Thrown if the token is not registered in the manager.</exception>
        public static AuthToken GetToken(string tokenId, string remoteAddress)
        {
            remoteAddress = (remoteAddress == "::1" ? "127.0.0.1" : remoteAddress);
            if (tokenId == null)
                throw new Exception("Invalid token");
            locker.Wait();
            try
            {
                if (tokens.ContainsKey(tokenId))
                {
                    if (remoteAddress != null && tokens[tokenId].RemoteAddress != remoteAddress)
                        throw new Exception("Invalid token");
                    return tokens[tokenId];
                }
                else
                    throw new Exception("Invalid token");
            }
            finally
            {
                locker.Release();
            }
        }

        /// <summary>
        /// Remove all expired tokens from the manager.
        /// </summary>
        public static void PruneExpiredTokens()
        {
            locker.Wait();
            try
            {
                foreach (var k in tokens.Keys.ToList())
                {
                    if (tokens[k].IsExpired)
                        tokens.Remove(k);
                }
            }
            finally
            {
                locker.Release();
            }
        }

        private static void Dispose()
        {
            locker.Dispose();
        }
    }

}