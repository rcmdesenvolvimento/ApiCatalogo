using ApiCatalogo.Context;
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCatalogo.ApiEndPoints
{
    public static class CategoriasEndPoints
    {
        public static void MapCategoriasEndPoints(this WebApplication app)
        {
            app.MapGet("/categorias", async (AppDbContext db) => await db.Categorias.ToListAsync())
                                                            .WithTags("Categorias")
                                                            .RequireAuthorization();

            app.MapGet("/categorias/{id:int}", async (int id, AppDbContext db) =>
                await db.Categorias.FindAsync(id) is Categoria categoria
                                                     ? Results.Ok(categoria)
                                                     : Results.NotFound("Categoria não encontrada."));
            //.WithTags("Categorias")
            //.RequireAuthorization(); ;

            app.MapPost("/categorias", async (Categoria categoria, AppDbContext db) =>
            {
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();
                return Results.Created($"/categorias/{categoria.CategoriaId}", categoria);
            });

            app.MapPut("/categorias/{id:int}", async (int id, Categoria categoria, AppDbContext db) =>
            {
                var _categoria = await db.Categorias.FindAsync(id);

                if (_categoria is null) return Results.NotFound("Categoria não encontrda");

                _categoria.Nome = categoria.Nome;
                _categoria.Descricao = categoria.Descricao;

                await db.SaveChangesAsync();

                return Results.Ok("Categoria alterada com sucesso.");

            });

            app.MapDelete("/categorias/{id:int}", async (int id, AppDbContext db) =>
            {
                var categoria = await db.Categorias.FindAsync(id);

                if (categoria is null)
                {
                    return Results.NotFound("Categoria não encontrada.");
                }

                db.Categorias.Remove(categoria);
                await db.SaveChangesAsync();

                return Results.Ok("Categoria excluída com sucesso.");
            });
        }
    }
}
