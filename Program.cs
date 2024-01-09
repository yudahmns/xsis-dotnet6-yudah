using System.ComponentModel.DataAnnotations;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MovieContext>(options => 
{
    options.UseNpgsql(@"Host=localhost:49153;Username=postgres;Password=postgrespw;Database=postgres");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x => 
{
    x.EnableAnnotations();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.MapGet("/Movies", (MovieContext movies) => {
    var list = movies.movie.ToList();
    return list;
});

app.MapGet("/Movies/{id}", (MovieContext movies, int id) => {
    var found = movies.movie.Find(id);
    if(found is null)
        return Results.NotFound($"{id} not found");
    return Results.Ok(found);
});

app.MapPost("/Movies", (MovieContext movies, movie movie) => {
    if (string.IsNullOrEmpty(movie.title))
        return Results.BadRequest("title cannot be null or empty");
    if (movie.rating is not null & 10 < movie.rating )
        return Results.BadRequest("rating cannot be greater than 10");
    if (movie.rating is not null & movie.rating < 0)
        return Results.BadRequest("rating cannot be smaller than 0");

    var found = movies.movie.Find(movie.id);
    if (found is not null)
        return Results.Conflict(movie);
    movie.created_at = DateTime.Now;
    movies.movie.Add(movie);
    movies.SaveChanges();
    return Results.Created($"/Movies/{movie.id}", movie);
});

app.MapMethods("/Movies/{id}", new string[] { "PATCH" }, (MovieContext movies, int id, movie movie) => {
    if (movie.title is not null && movie.title == string.Empty)
        return Results.BadRequest("title cannot be empty");
    if (movie.rating is not null & 10 < movie.rating )
        return Results.BadRequest("rating cannot be greater than 10");
    if (movie.rating is not null & movie.rating < 0)
        return Results.BadRequest("rating cannot be smaller than 0");

    var found = movies.movie.Find(id);
    if (found is null)
        return Results.NotFound($"{id} not found");
    if (!string.IsNullOrEmpty(movie.title))
        found.title = movie.title;
    if (!string.IsNullOrEmpty(movie.image))
        found.image = movie.image;
    if (movie.rating is not null)
        found.rating = movie.rating;
    found.updated_at = DateTime.Now;
    movies.SaveChanges();
    return Results.NoContent();
});

app.MapDelete("/Movies/{id}", (MovieContext movies, int id) => {
    var found = movies.movie.Find(id);
    if (found is null)
        return Results.NotFound($"{id} not found");
    movies.Remove(found);
    movies.SaveChanges();
    return Results.NoContent();
});

app.Run();

public class movie
{
    [SwaggerSchema(ReadOnly = true)]
    public int id { get; set; }

    [Required]
    public string? title { get; set; }

    [Range(0, 10)]
    public decimal? rating { get; set; }

    public string? image { get; set; }

    [SwaggerSchema(ReadOnly = true)]
    [DataType(DataType.DateTime)]
    public DateTime created_at { get; set; }

    [SwaggerSchema(ReadOnly = true)]
    [DataType(DataType.DateTime)]
    public DateTime? updated_at { get; set; }
}
public class MovieContext : DbContext
{
    public MovieContext (DbContextOptions<MovieContext> options) : base(options){}
    public DbSet<movie> movie { get; set; }
}
