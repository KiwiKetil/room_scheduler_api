﻿using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomSchedulerAPI.Features.Models.DTOs.UserDTOs;
using RoomSchedulerAPI.Features.Repositories.Interfaces;
using RoomSchedulerAPI.Features.Services.Interfaces;

namespace RoomSchedulerAPI.Features.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // admin only
        app.MapGet("/api/v1/users", async (IUserService userService, ILogger<Program> logger, [AsParameters] UserQuery query) =>
        {
            logger.LogDebug("Retrieving all users");

            var (users, totalCount) = await userService.GetUsersAsync(query);
            if (!users.Any())
            {
                return Results.NotFound("No users found");
            }
            logger.LogDebug($"count is: {totalCount}");

            return Results.Ok(new
            {
                TotalCount = totalCount,
                Data = users                
            });
        })
        //.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("GetAllUsers");

        // admin only
        app.MapGet("/api/v1/users/{id}", async ([FromRoute] Guid id, IUserService userService, ILogger<Program> logger) => // async is for everything inside the body
        {
            logger.LogDebug("Retrieving user with ID {userId}", id);

            var user = await userService.GetUserByIdAsync(id);
            return user != null ? Results.Ok(user) : Results.NotFound("User was not found");
        })
        //.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("GetUserById");

        app.MapPut("/api/v1/users/{id}", async ([FromRoute] Guid id, [FromBody] UserUpdateDTO dto, IUserService userService, IValidator<UserUpdateDTO> validator, ILogger<Program> logger) =>
        {
            logger.LogDebug("Updating user with ID {userId}", id);

            var validationResult = validator.Validate(dto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Results.BadRequest(errors);
            }

            var user = await userService.UpdateUserAsync(id, dto);
            return user != null ? Results.Ok(user) : Results.Problem(
                title: "An issue occured",
                statusCode: 409,
                detail: "User could not be updated"
                );
        })
        //.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin, User" }) // user only self
        .WithName("UpdateUser");

        // admin only
        app.MapDelete("/api/v1/users/{id}", async ([FromRoute] Guid id, IUserService userService, ILogger<Program> logger) =>
        {
            logger.LogDebug("Deleting user with ID {userId}", id);

            var user = await userService.DeleteUserAsync(id);
            return user != null ? Results.Ok(user) : Results.Problem(
                title: "An issue occured",
                statusCode: 409,
                detail: "User could not be deleted"
                );
        })
        //.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }) // user only self? Or only admin can delete?
        .WithName("DeleteUser");

        // admin only
        app.MapPost("/api/v1/users/register", async ([FromBody] UserRegistrationDTO dto, IValidator<UserRegistrationDTO> validator, IUserService userService, ILogger<Program> logger) =>
        {
            logger.LogDebug("Registering new user");

            var validationResult = validator.Validate(dto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Results.BadRequest(errors);
            }

            var res = await userService.RegisterUserAsync(dto);

            return res != null ? Results.Ok(res) : Results.Conflict(new { Message = "User already exists" });
        })
        .WithName("RegisterUser");
        // .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });


        // new users must change passwordgiven by admin(?)
        app.MapPost("/api/v1/users/change-password", async ([FromBody] ChangePasswordDTO dto, IValidator <ChangePasswordDTO> validator, IUserService userService, ILogger<Program> logger) =>
        {
            logger.LogDebug("User changing password");

            var validationResult = validator.Validate(dto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Results.BadRequest(errors);
            }

            var res = await userService.ChangePasswordAsync(dto);

            return res 
            ? Results.Ok(new { Message = "Password changed successfully." })
            : Results.BadRequest(new { Message = "Password could not be changed. Please check your username or password and try again." });
        })
        //.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin, User" }) // user only self
        .WithName("ChangePassword");

        app.MapPost("/api/v1/login", async ([FromBody] LoginDTO dto, IValidator<LoginDTO> validator, IUserAuthenticationService authService, ITokenGenerator tokenGenerator, ILogger<Program> logger) =>
        {
            logger.LogDebug("User logging in");

            var validationResult = validator.Validate(dto);
            if (!validationResult.IsValid) 
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Results.BadRequest(errors);
            }

            var authenticatedUser = await authService.AuthenticateUserAsync(dto);

            if (authenticatedUser == null) 
            {
                return Results.Problem("Login failed. Please check your username and/or password and try again.", statusCode: 401);
            }

            var token = await tokenGenerator.GenerateTokenAsync(authenticatedUser);

            return Results.Ok(new { Token = token });            
        })
        .WithName("UserLogin");
    }
}
