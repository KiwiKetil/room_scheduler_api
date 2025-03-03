﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RoomSchedulerAPI.Features.Models.Entities;
using RoomSchedulerAPI.Features.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RoomSchedulerAPI.UnitTests.ServicesTests;
public class TokenGeneratorTests
{
    private readonly TokenGenerator _service;
    private readonly Mock<ILogger<TokenGenerator>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    public TokenGeneratorTests()
    {        
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TheIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TheAudience");
        _configMock.Setup(c => c["Jwt:Key"]).Returns("TheSecretKeyIsNowGreaterThan256Bits");

        _service = new TokenGenerator(_configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GenerateToken_WhenUserIsNull_ThrowsArgumentException()
    {
        // Arrange
        List<UserRole> roles = [new UserRole { RoleName = "User" }];

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.GenerateToken(null!, false, roles));
        Assert.Equal("An authenticated user is needed", exception.Message); 
    }

    [Fact]
    public void GenerateToken_WhenRolesAreNullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var authenticatedUser = new User { Id = UserId.NewId, Email = "email@123@gmail.com" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.GenerateToken(authenticatedUser, false, []));
        Assert.Equal("Role(s) are needed", exception.Message);
    }

    [Fact]
    public void GenerateToken_WhenJwtKeyIsNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var authenticatedUser = new User { Id = UserId.NewId, Email = "email@123@gmail.com" };
        List<UserRole> roles = [new UserRole { RoleName = "User" }];

        _configMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _service.GenerateToken(authenticatedUser, false, roles));
        Assert.Equal("Missing configuration: Jwt:Key", exception.Message);
    }

    [Fact]
    public void GenerateToken_WhenJwtIssuerIsNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var authenticatedUser = new User { Id = UserId.NewId, Email = "email@123@gmail.com" };
        List<UserRole> roles = [new UserRole { RoleName = "User" }];

        _configMock.Setup(c => c["Jwt:Issuer"]).Returns((string?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _service.GenerateToken(authenticatedUser, false, roles));
        Assert.Equal("Missing configuration: Jwt:Issuer", exception.Message);
    }

    [Fact]
    public void GenerateToken_WhenJwtAudienceIsNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var authenticatedUser = new User { Id = UserId.NewId, Email = "email@123@gmail.com" };
        List<UserRole> roles = [new UserRole { RoleName = "User" }];

        _configMock.Setup(c => c["Jwt:Audience"]).Returns((string?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _service.GenerateToken(authenticatedUser, false, roles));
        Assert.Equal("Missing configuration: Jwt:Audience", exception.Message);
    }

    [Fact]
    public void GenerateToken_WhenSuccess_WhenUserHasNotUpdatedPassword_ReturnsValidTokenWithCorrectClaims()
    {
        // Arrange
        var authenticatedUser = new User { Id = UserId.NewId, Email = "email@123@gmail.com" };
        List<UserRole> roles = [new UserRole { RoleName = "User" }];
        var expectedRoles = roles.Select(r => r.RoleName).ToList();
        var passwordUpdated = false;

        // Act
        var result = _service.GenerateToken(authenticatedUser, passwordUpdated, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result);
        var idClaim = jwtToken.Subject;
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name);
        var rolesClaim = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        var passwordUpdatedClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "passwordUpdated");

        Assert.IsType<string>(result);
        Assert.Equal(authenticatedUser.Id.Value.ToString(), idClaim);
        Assert.NotNull(nameClaim);
        Assert.Equal(authenticatedUser.Email, nameClaim.Value);
        Assert.Equal(expectedRoles, rolesClaim);
        Assert.Contains("TheIssuer", jwtToken.Issuer);
        Assert.Contains("TheAudience", jwtToken.Audiences);
        Assert.NotNull(passwordUpdatedClaim);
        Assert.False(bool.Parse(passwordUpdatedClaim.Value));
    }

    [Fact]
    public void GenerateToken_WhenSuccess_WhenUserHasUpdatedPassword_ReturnsValidTokenWithCorrectClaims()
    {
        // Arrange
        var authenticatedUser = new User { Id = UserId.NewId, Email = "email@123@gmail.com" };
        List<UserRole> roles = [
            new UserRole { RoleName = "User" },
            new UserRole {RoleName = "Admin" } 
        ];
        var expectedRoles = roles.Select(r => r.RoleName).ToList();
        var passwordUpdated = true;

        // Act
        var result = _service.GenerateToken(authenticatedUser, passwordUpdated, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result);
        var idClaim = jwtToken.Subject;
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name);
        var rolesClaim = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(r => r.Value).ToList();
        var rolesOutput = string.Join(", ", rolesClaim);

        // Write to the console
        Console.WriteLine($"Roles: {rolesOutput}");
        var passwordUpdatedClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "passwordUpdated");

        Assert.IsType<string>(result);
        Assert.Equal(authenticatedUser.Id.Value.ToString(), idClaim);
        Assert.NotNull(nameClaim);
        Assert.Equal(authenticatedUser.Email, nameClaim.Value);
        Assert.Equal(expectedRoles, rolesClaim);
        Assert.Contains("TheIssuer", jwtToken.Issuer);
        Assert.Contains("TheAudience", jwtToken.Audiences);
        Assert.NotNull(passwordUpdatedClaim);
        Assert.True(bool.Parse(passwordUpdatedClaim.Value));
    }
}
