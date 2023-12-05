/*Ricardo Miranda*/
using ApiCatalogo.Context;
using ApiCatalogo.Models;
using ApiCatalogo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiCatalogo", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = @"JWT Authorization header using the Bearer scheme.
                    Enter 'Bearer'[space].Example: \'Bearer 12345abcdef\'",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                          {
                              Reference = new OpenApiReference
                              {
                                  Type = ReferenceType.SecurityScheme,
                                  Id = "Bearer"
                              }
                          },
                         new string[] {}
                    }
                });
});

var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
                                            options
                                            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Token
builder.Services.AddSingleton<ITokenService>(new TokenService());


#region(Validação do Token)
builder.Services.AddAuthentication
                 (JwtBearerDefaults.AuthenticationScheme)
                 .AddJwtBearer(options =>
                 {
                     options.TokenValidationParameters = new TokenValidationParameters
                     {
                         ValidateIssuer = true,
                         ValidateAudience = true,
                         ValidateLifetime = true,
                         ValidateIssuerSigningKey = true,

                         ValidIssuer = builder.Configuration["Jwt:Issuer"],
                         ValidAudience = builder.Configuration["Jwt:Audience"],
                         IssuerSigningKey = new SymmetricSecurityKey
                         (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                     };
                 });

builder.Services.AddAuthorization();

#endregion()

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Catalogo de Produtos 2023");

#region(Endpoint para login)
app.MapPost("/login", [AllowAnonymous] (User user, ITokenService tokenService) =>
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
#endregion()

#region(Categorias)

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
#endregion

#region(Produtos)

app.MapPost("/produtos", async (Produto produto, AppDbContext db) =>
{
    var _CategoriaId = await db.Categorias.FindAsync(produto.CategoriaId);

    if (_CategoriaId is null) return Results.NotFound("Categoria não cadastrada.");

    db.Produtos.Add(produto);
    await db.SaveChangesAsync();
    return Results.Created($"/produtos/{produto.ProdutoId}", produto);
});

app.MapGet("/produtos", async (AppDbContext db) => await db.Produtos.ToListAsync())
                                                                    .WithTags("Produtos")
                                                                    .RequireAuthorization();

app.MapGet("/produtos/{id:int}", async (int id, AppDbContext db)
    =>
{
    return await db.Produtos.FindAsync(id)
                 is Produto produto
                 ? Results.Ok(produto)
                 : Results.NotFound();
});

app.MapPut("/produtos/{id:int}", async (int id, Produto produto, AppDbContext db) =>
{

    if (produto.ProdutoId != id)
    {
        return Results.BadRequest();
    }

    var produtoDB = await db.Produtos.FindAsync(id);

    if (produtoDB is null) return Results.NotFound();

    produtoDB.Nome = produto.Nome;
    produtoDB.Descricao = produto.Descricao;
    produtoDB.Preco = produto.Preco;
    produtoDB.Imagem = produto.Imagem;
    produtoDB.DataCompra = produto.DataCompra;
    produtoDB.Estoque = produto.Estoque;
    produtoDB.CategoriaId = produto.CategoriaId;

    await db.SaveChangesAsync();

    return Results.Ok(produtoDB);
});

app.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) =>
{
    var produto = await db.Produtos.FindAsync(id);

    if (produto is null)
    {
        return Results.NotFound();
    }

    db.Produtos.Remove(produto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

#endregion

app.UseAuthentication();
app.UseAuthorization();

app.Run();

