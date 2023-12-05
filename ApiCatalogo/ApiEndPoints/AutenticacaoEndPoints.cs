using ApiCatalogo.Models;
using ApiCatalogo.Services;
using Microsoft.AspNetCore.Authorization;

namespace ApiCatalogo.ApiEndPoints
{
    public static class AutenticacaoEndPoints
    {
        public static void MapAutenticacaoEndPoints(this WebApplication app)
        {
            app.MapPost("/login", [AllowAnonymous] (UserModel user, ITokenService tokenService) =>
            {
                if (user == null)
                {
                    return Results.BadRequest("Login Inválido");
                }
                if (user.UserName == "ricardo" && user.Password == "@rcm#123")
                {
                    var tokenString = tokenService.GerarToken(app.Configuration["Jwt:Key"],
                        app.Configuration["Jwt:Issuer"],
                        app.Configuration["Jwt:Audience"],
                        user);
                    return Results.Ok(new { token = tokenString });
                }
                else
                {
                    return Results.BadRequest("Login Inválido");
                }
            }).Produces(StatusCodes.Status400BadRequest)
              .Produces(StatusCodes.Status200OK)
              .WithName("Login")
              .WithTags("Autenticacao");
        }
    }
}
