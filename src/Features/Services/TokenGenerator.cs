﻿using Microsoft.IdentityModel.Tokens;
using RoomSchedulerAPI.Features.Models.Entities;
using RoomSchedulerAPI.Features.Repositories.Interfaces;
using RoomSchedulerAPI.Features.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RoomSchedulerAPI.Features.Services;

public class TokenGenerator(IConfiguration config, ILogger<TokenGenerator> logger) : ITokenGenerator
{
    private readonly IConfiguration _config = config;
    private readonly ILogger _logger = logger;

    public string GenerateToken(User authenticatedUser, bool hasUpdatedPassword, IEnumerable<UserRole> userRoles)
    {
        if (authenticatedUser == null)
        {
            throw new ArgumentException("An authenticated user is needed to create a token");
        }

        userRoles ??= [];

        _logger.LogDebug("Generating token for user ID: {UserId}", authenticatedUser.Id);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var userId = authenticatedUser.Id;
        var userName = authenticatedUser.Email;

        List<Claim> claims = [];
        claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()));
        claims.Add(new Claim(JwtRegisteredClaimNames.Name, userName.ToString()));
        claims.Add(new Claim("passwordUpdated", hasUpdatedPassword ? "true" : "false"));

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
        }

        var token = new JwtSecurityToken
            (
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(240),
            signingCredentials: credentials
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
