using ApiCatalogo.Context;
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
                                            options
                                            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Catalogo de Produtos 2023");

app.MapGet("/categorias", async (AppDbContext db) => await db.Categorias.ToListAsync());


app.MapGet("/categorias/{id:int}", async (int id, AppDbContext db) =>
    await db.Categorias.FindAsync(id) is Categoria categoria ? Results.Ok(categoria) : Results.NotFound("Categoria não encontrada."));


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

app.Run();

