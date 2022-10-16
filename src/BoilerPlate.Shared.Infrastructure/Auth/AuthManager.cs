﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BoilerPlate.Shared.Abstraction.Auth;
using BoilerPlate.Shared.Abstraction.Time;
using Microsoft.IdentityModel.Tokens;

namespace BoilerPlate.Shared.Infrastructure.Auth;

internal sealed class AuthManager : IAuthManager
{
    private readonly AuthOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials = null!;
    private readonly string? _issuer;

    public AuthManager(AuthOptions options, IClock clock)
    {
        var issuerSigningKey = options.IssuerSigningKey;
        if (issuerSigningKey is null)
        {
            throw new InvalidOperationException("Issuer signing key not set.");
        }

        _options = options;
        _clock = clock;
        if (string.IsNullOrWhiteSpace(options.IssuerSigningKey))
            _signingCredentials =
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.IssuerSigningKey!)),
                    SecurityAlgorithms.HmacSha256);
        _issuer = options.Issuer;
    }

    public JsonWebToken CreateToken(Guid userId, string refreshToken, string? role, string? audience,
        IDictionary<string, IEnumerable<string>>? claims)
    {
        var now = _clock.CurrentDate();
        var jwtClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeMilliseconds().ToString())
        };

        if (!string.IsNullOrWhiteSpace(role))
            jwtClaims.Add(new Claim(ClaimTypes.Role, role));

        if (!string.IsNullOrWhiteSpace(audience))
            jwtClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));

        if (claims?.Any() is true)
        {
            var customClaims = new List<Claim>();
            foreach (var (claim, values) in claims)
                customClaims.AddRange(values.Select(value => new Claim(claim, value)));

            jwtClaims.AddRange(customClaims);
        }

        var expires = now.Add(_options.Expiry);

        var jwt = new JwtSecurityToken(
            _issuer,
            claims: jwtClaims,
            notBefore: now,
            expires: expires,
            signingCredentials: _signingCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt)!;

        var jsonWebToken = new JsonWebToken
        {
            AccessToken = token,
            Expiry = new DateTimeOffset(expires).ToUnixTimeMilliseconds(),
            RefreshToken = refreshToken,
            UserId = userId
        };

        if (claims is null) return jsonWebToken;
        foreach (var item in claims)
            jsonWebToken.Claims.Add(item);

        return jsonWebToken;
    }
}