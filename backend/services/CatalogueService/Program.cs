using CatalogueService.Data;
using CatalogueService.Events;
using CatalogueService.Middleware;
using CatalogueService.Repositories;
using CatalogueService.Services;
using CatalogueService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.WithProperty("Service", "catalogue-service")
       .Enrich.FromLogContext()
       .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddHostedService<CatalogueService.Events.ReviewCreatedConsumer>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogueService", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Movies.Any())
    {
        db.Movies.AddRange(
            new Movie { Title = "Inception", Year = 2010, Genre = "Sci-Fi", Director = "Christopher Nolan", Description = "A thief who steals corporate secrets through dream-sharing technology." },
            new Movie { Title = "The Matrix", Year = 1999, Genre = "Sci-Fi", Director = "The Wachowskis", Description = "A hacker discovers that reality is a simulation." },
            new Movie { Title = "Interstellar", Year = 2014, Genre = "Sci-Fi", Director = "Christopher Nolan", Description = "A team of explorers travel through a wormhole in space." },
            new Movie { Title = "The Dark Knight", Year = 2008, Genre = "Action", Director = "Christopher Nolan", Description = "Batman faces the Joker, a criminal mastermind." },
            new Movie { Title = "Parasite", Year = 2019, Genre = "Thriller", Director = "Bong Joon-ho", Description = "A poor family schemes to become employed by a wealthy family." }
        );
        await db.SaveChangesAsync();
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogueService v1"));
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
