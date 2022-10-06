﻿using System.Text;
using BoilerPlate.Shared.Abstraction.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BoilerPlate.Shared.Infrastructure.Auth;

public static class Extensions
{
    private const string AccessTokenCookieName = "__access-token";
    private const string ClientIdHeaderName = "x-client-id";
    private const string AuthorizationHeader = "authorization";

    public static void AddAuth(this IServiceCollection services,
        Action<JwtBearerOptions>? optionsFactory)
    {
        var options = services.GetOptions<AuthOptions>("auth");
        services.AddSingleton<IAuthManager, AuthManager>();

        if (options.AuthenticationDisabled)
        {
            services.AddSingleton<IPolicyEvaluator, DisabledAuthenticationPolicyEvaluator>();
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            RequireAudience = options.RequireAudience,
            ValidIssuer = options.ValidIssuer,
            ValidIssuers = options.ValidIssuers,
            ValidateActor = options.ValidateActor,
            ValidAudience = options.ValidAudience,
            ValidAudiences = options.ValidAudiences,
            ValidateAudience = options.ValidateAudience,
            ValidateIssuer = options.ValidateIssuer,
            ValidateLifetime = options.ValidateLifetime,
            ValidateTokenReplay = options.ValidateTokenReplay,
            ValidateIssuerSigningKey = options.ValidateIssuerSigningKey,
            SaveSigninToken = options.SaveSigninToken,
            RequireExpirationTime = options.RequireExpirationTime,
            RequireSignedTokens = options.RequireSignedTokens,
            ClockSkew = TimeSpan.Zero
        };

        if (string.IsNullOrWhiteSpace(options.IssuerSigningKey))
        {
            throw new ArgumentException("Missing issuer signing key.", nameof(options.IssuerSigningKey));
        }

        if (!string.IsNullOrWhiteSpace(options.AuthenticationType))
        {
            tokenValidationParameters.AuthenticationType = options.AuthenticationType;
        }

        var rawKey = Encoding.UTF8.GetBytes(options.IssuerSigningKey);
        tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(rawKey);

        if (!string.IsNullOrWhiteSpace(options.NameClaimType))
        {
            tokenValidationParameters.NameClaimType = options.NameClaimType;
        }

        if (!string.IsNullOrWhiteSpace(options.RoleClaimType))
        {
            tokenValidationParameters.RoleClaimType = options.RoleClaimType;
        }

        services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.Authority = options.Authority;
                o.Audience = options.Audience;
                o.MetadataAddress = options.MetadataAddress!;
                o.SaveToken = options.SaveToken;
                o.RefreshOnIssuerKeyNotFound = options.RefreshOnIssuerKeyNotFound;
                o.RequireHttpsMetadata = options.RequireHttpsMetadata;
                o.IncludeErrorDetails = options.IncludeErrorDetails;
                o.TokenValidationParameters = tokenValidationParameters;
                if (!string.IsNullOrWhiteSpace(options.Challenge))
                {
                    o.Challenge = options.Challenge;
                }

                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (!context.Request.Headers.TryGetValue(ClientIdHeaderName, out var clientId))
                        {
                            context.Fail("Invalid");
                            return Task.CompletedTask;
                        }

                        // get client id from claims
                        var claim = context.Principal!.Claims.First(e => e.Type == "ci");
                        if (claim.Value != clientId.ToString())
                            context.Fail("Invalid");

                        return Task.CompletedTask;
                    },
                };

                optionsFactory?.Invoke(o);
            });

        services.AddSingleton(options);
        if (options.Cookie is not null)
            services.AddSingleton(options.Cookie);
        services.AddSingleton(tokenValidationParameters);
        services.AddAuthorization(authorization =>
        {
            if (options.Policies is null) return;
            foreach (var policy in options.Policies)
                authorization.AddPolicy(policy, x => x.RequireClaim("permissions", policy));
        });
    }

    public static void UseAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Headers.ContainsKey(AuthorizationHeader))
            {
                ctx.Request.Headers.Remove(AuthorizationHeader);
            }

            if (ctx.Request.Cookies.ContainsKey(AccessTokenCookieName))
            {
                var authenticateResult = await ctx.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                if (authenticateResult.Succeeded)
                {
                    ctx.User = authenticateResult.Principal;
                }
            }

            await next();
        });
    }
}