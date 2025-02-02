﻿namespace CafeBazaar.DeveloperApi
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Olive;
    using System;
    using System.Threading.Tasks;

    public class CafeBazaarDeveloperService
    {
        readonly IHttpContextAccessor HttpContextAccessor;
        readonly CafeBazaarOptions Options;
        readonly ICafeBazaarTokenStorage TokenStorage;
        readonly WebApiInvoker WebApiInvoker;

        public CafeBazaarDeveloperService(IHttpContextAccessor httpContextAccessor, IOptionsSnapshot<CafeBazaarOptions> options, ICafeBazaarTokenStorage tokenStorage)
        {
            HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            TokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            WebApiInvoker = new WebApiInvoker(Options.BaseUri);
        }

        public async Task<bool> IsAuthorizationRequired()
        {
            return (await TokenStorage.GetAccessToken()).IsEmpty();
        }

        public Task<string> GetAuthorizationUri()
        {
            return Task.FromResult(
                $"{Options.BaseUri}devapi/v2/auth/authorize/?response_type=code&access_type=offline&redirect_uri={GetAbsoluteRedirectUri()}&client_id={Options.ClientId}"
            );
        }

        public async Task HandleAuthorizationCallback(string code)
        {
            var path = "devapi/v2/auth/token/";

            var request = new CafeBazaarObtainTokenRequest
            {
                Code = code,
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                RedirectUri = GetAbsoluteRedirectUri()
            };

            await request.Validate();

            var result = await WebApiInvoker.PostForm<CafeBazaarObtainTokenResult>(path, request);

            result.EnsureSucceeded();

            await TokenStorage.Save(result.AccessToken, result.ExpiresIn, result.RefreshToken);
        }

        public async Task<CafeBazaarValidatePurchaseResult> ValidatePurchase(CafeBazaarValidatePurchaseRequest request)
        {
            await request.Validate();

            await EnsureAccessTokenValidity();

            var path = $"devapi/v2/api/validate/{request.PackageName}/inapp/{request.ProductId}/purchases/{request.PurchaseToken}/?access_token={await TokenStorage.GetAccessToken()}";

            var result = await WebApiInvoker.Get<CafeBazaarValidatePurchaseResult>(path);

            result.EnsureSucceeded();

            return result;
        }

        public async Task<CafeBazaarValidateSubscriptionResult> ValidateSubscription(CafeBazaarValidateSubscriptionRequest request)
        {
            await request.Validate();

            await EnsureAccessTokenValidity();

            var path = $"devapi/v2/api/applications/{request.PackageName}/subscriptions/{request.SubscriptionId}/purchases/{request.PurchaseToken}/?access_token={await TokenStorage.GetAccessToken()}";

            var result = await WebApiInvoker.Get<CafeBazaarValidateSubscriptionResult>(path);

            result.EnsureSucceeded();

            return result;
        }

        public async Task<CafeBazaarCancelSubscriptionResult> CancelSubscription(CafeBazaarCancelSubscriptionRequest request)
        {
            await request.Validate();

            await EnsureAccessTokenValidity();

            var path = $"devapi/v2/api/applications/{request.PackageName}/subscriptions/{request.SubscriptionId}/purchases/{request.PurchaseToken}/cancel/?access_token={await TokenStorage.GetAccessToken()}";

            var result = await WebApiInvoker.Get<CafeBazaarCancelSubscriptionResult>(path);

            result.EnsureSucceeded();

            return result;
        }

        async Task EnsureAccessTokenValidity()
        {
            if ((await TokenStorage.GetRefreshToken()).IsEmpty())
                throw new Exception("First of all you need to authorize against Cafe Bazzar.");

            if (await TokenStorage.AccessTokenExpired())
                await RenewAccessToken();
        }

        async Task RenewAccessToken()
        {
            var path = "devapi/v2/auth/token/";

            var request = new CafeBazaarRenewTokenRequest
            {
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                RefreshToken = Options.RefreshToken
            };

            await request.Validate();

            var result = await WebApiInvoker.PostForm<CafeBazaarRenewTokenResult>(path, request);

            result.EnsureSucceeded();

            await TokenStorage.Renew(result.AccessToken, result.ExpiresIn);
        }

        string GetAbsoluteRedirectUri()
        {
            var redirectUri = new Uri(Options.RedirectPath, UriKind.RelativeOrAbsolute);

            if (redirectUri.IsAbsoluteUri) return redirectUri.ToString();

            var request = HttpContextAccessor.HttpContext.Request;
            return new Uri(new Uri($"{request.Scheme}://{request.Host}"), redirectUri).ToString();
        }
    }
}
