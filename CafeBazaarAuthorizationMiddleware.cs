﻿namespace CafeBazaar.DeveloperApi
{
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Olive;

    public class CafeBazaarAuthorizationMiddleware
    {
        readonly RequestDelegate Next;

        public CafeBazaarAuthorizationMiddleware(RequestDelegate next)
        {
            this.Next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context, IOptionsSnapshot<CafeBazaarOptions> options, CafeBazaarDeveloperService developerService)
        {
            var pathMatched = context.Request.Path.StartsWithSegments(options.Value.RedirectUri.AbsolutePath);
            var code = context.Request.Query["code"].FirstOrDefault();

            if (pathMatched && code.HasValue())
            {
                await developerService.HandleAuthorizationCallback(code);
                await context.Response.WriteAsync("Cafe Bazaar authorization callback executed.");
                return;
            }

            await Next(context);
        }
    }
}
