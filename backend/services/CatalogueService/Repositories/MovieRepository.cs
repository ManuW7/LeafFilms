using CatalogueService.Data;
using CatalogueService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogueService.Repositories;

public interface IMovieRepository
{
    Task<IEnumerable<Movie>> GetAllAsync();
    Task<Movie?> GetByIdAsync(Guid id);
    Task<IEnumerable<Movie>> SearchAsync(string query);
    Task<Movie> CreateAsync(Movie movie);
    Task<Movie> UpdateAsync(Movie movie);
    Task DeleteAsync(Guid id);
}

public class MovieRepository : IMovieRepository
{
    private readonly AppDbContext _db;

    public MovieRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Movie>> GetAllAsync()
        => await _db.Movies.AsNoTracking().OrderByDescending(m => m.CreatedAt).ToListAsync();

    public async Task<Movie?> GetByIdAsync(Guid id)
        => await _db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<Movie>> SearchAsync(string query)
        => await _db.Movies.AsNoTracking()
               .Where(m => EF.Functions.ILike(m.Title, $"%{query}%")
                        || (m.TitleRu != null && EF.Functions.ILike(m.TitleRu, $"%{query}%"))
                        || EF.Functions.ILike(m.Director, $"%{query}%")
                        || EF.Functions.ILike(m.Genre, $"%{query}%"))
               .ToListAsync();

    public async Task<Movie> CreateAsync(Movie movie)
    {
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();
        return movie;
    }

    public async Task<Movie> UpdateAsync(Movie movie)
    {
        _db.Movies.Update(movie);
        await _db.SaveChangesAsync();
        return movie;
    }

    public async Task DeleteAsync(Guid id)
    {
        var movie = await _db.Movies.FindAsync(id);
        if (movie is not null) { _db.Movies.Remove(movie); await _db.SaveChangesAsync(); }
    }
}
