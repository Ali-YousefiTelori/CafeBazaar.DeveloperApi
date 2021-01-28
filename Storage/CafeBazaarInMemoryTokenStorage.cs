﻿namespace CafeBazaar.DeveloperApi
{
    using Olive;
    using System;
    using System.Threading.Tasks;

    public class CafeBazaarInMemoryTokenStorage : CafeBazaarTokenStorageBase
    {
        private readonly CafeBazaarToken _token;

        public CafeBazaarInMemoryTokenStorage() => _token = new CafeBazaarToken();

        public override Task Save(string accessToken, TimeSpan expiresIn, string refreshToken)
        {
            if (accessToken.IsEmpty()) throw new ArgumentNullException(nameof(accessToken));

            if (expiresIn == TimeSpan.MinValue) throw new ArgumentException("Already expired!", nameof(expiresIn));

            if (refreshToken.IsEmpty()) throw new ArgumentNullException(nameof(refreshToken));

            _token.AccessToken = accessToken;
            _token.ExpiresAt = expiresIn.ToDate();
            _token.RefreshToken = refreshToken;

            return Task.CompletedTask;
        }

        public override Task Renew(string accessToken, TimeSpan expiresIn)
        {
            if (accessToken.IsEmpty()) throw new ArgumentNullException(nameof(accessToken));

            if (expiresIn == TimeSpan.MinValue) throw new ArgumentException("Already expired!", nameof(expiresIn));

            _token.AccessToken = accessToken;
            _token.ExpiresAt = expiresIn.ToDate();

            return Task.CompletedTask;
        }

        protected override Task<CafeBazaarToken> GetToken() => Task.FromResult(_token);
    }
}
