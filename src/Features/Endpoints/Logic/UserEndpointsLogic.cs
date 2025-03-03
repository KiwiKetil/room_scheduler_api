﻿using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomSchedulerAPI.Features.Models.DTOs.ResponseDTOs;
using RoomSchedulerAPI.Features.Models.DTOs.UserDTOs;
using RoomSchedulerAPI.Features.Models.Entities;
using RoomSchedulerAPI.Features.Repositories.Interfaces;
using RoomSchedulerAPI.Features.Services.Interfaces;
using System.Security.Claims;

namespace RoomSchedulerAPI.Features.Endpoints.Logic;

public static class UserEndpointsLogic
{
    public static async Task<IResult> GetUsersLogicAsync(
        [AsParameters] UserQuery query,
        IUserService userService,
        ILogger<Program> logger)
    {
        logger.LogDebug("Retrieving users");

        var usersAndCountDTO = await userService.GetUsersAsync(query);
        if (!usersAndCountDTO.UserDTOs.Any())
        {
            return Results.NotFound(new ErrorResponse(Message: "No users found"));
        }

        return Results.Ok(usersAndCountDTO);
    }

    public static async Task<IResult> GetUserByIdLogicAsync(
        [FromRoute] Guid id,
        IUserService userService,
        IAuthorizationService authorizationService,
        ClaimsPrincipal claims,
        ILogger<Program> logger)
    {
        logger.LogDebug("Retrieving user with ID {userId}", id);   

        var authorizationResult = await authorizationService.AuthorizeAsync(claims, id, "UserIdAccessPolicy");
        if (!authorizationResult.Succeeded)
        {
            return Results.Forbid();
        }

        var userDTO = await userService.GetUserByIdAsync(id);
        return userDTO != null 
            ? Results.Ok(userDTO)
            : Results.NotFound(new ErrorResponse(Message: "User was not found"));
    }

    public static async Task<IResult> UpdateUserLogicAsync(
        [FromBody] UserUpdateDTO dto,
        [FromRoute] Guid id,
        IUserService userService,
        IAuthorizationService authorizationService,
        ClaimsPrincipal claims,
        ILogger<Program> logger)
    {
        logger.LogDebug("Updating user with ID {userId}", id);

        var authorizationResult = await authorizationService.AuthorizeAsync(claims, id, "UserIdAccessPolicy");
        if (!authorizationResult.Succeeded)
        {
            return Results.Forbid();
        }

        var userDTO = await userService.UpdateUserAsync(id, dto);
        return userDTO != null 
            ? Results.Ok(userDTO)
            : Results.Conflict(new ErrorResponse(Message: "User could not be updated"));
    }

    public static async Task<IResult> DeleteUserLogicAsync(
        [FromRoute] Guid id,
        IUserService userService,
        ILogger<Program> logger)
    {
        logger.LogDebug("Deleting user with ID {userId}", id);

        var userDTO = await userService.DeleteUserAsync(id);
        return userDTO != null 
            ? Results.Ok(userDTO)
            : Results.Conflict(new ErrorResponse(Message: "User could not be deleted"));
    }

    public static async Task<IResult> RegisterUserLogicAsync(
        [FromBody] UserRegistrationDTO dto,
        IUserService userService,
        ILogger<Program> logger)
    {
        logger.LogDebug("Registering new user");

        var userDTO = await userService.RegisterUserAsync(dto);
        return userDTO != null 
            ? Results.Ok(userDTO)
            : Results.Conflict(new ErrorResponse(Message: "User could not be registered"));
    }

    public static async Task<IResult> UserLoginLogicAsync(
        [FromBody] LoginDTO dto,
        IUserService userService,
        ILogger<Program> logger)
    {
        logger.LogDebug("User logging in");

        var token = await userService.UserLoginAsync(dto);
        return token != null
            ? Results.Ok(new TokenResponse(Token: token))
            : Results.Unauthorized();
    }

    public static async Task<IResult> UpdatePasswordLogicAsync(
        [FromBody] UpdatePasswordDTO dto,
        IUserService userService,
        IAuthorizationService authorizationService,
        ClaimsPrincipal claims,
        ILogger<Program> logger)
    {
        logger.LogDebug("User updating password");

        var authorizationResult = await authorizationService.AuthorizeAsync(claims, dto, "UserNameAccessPolicy");
        if (!authorizationResult.Succeeded)
        {
            return Results.Forbid();
        }

        var token = await userService.UpdatePasswordAsync(dto);

        return token != null
            ? Results.Ok(new TokenResponse(Token: token)) 
            : Results.Unauthorized();
    }
}
