using System.Security.Cryptography;
using System.Text;
using UserService.DTOs.Commands;
using UserService.DTOs.Queries;
using UserService.Exceptions;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public interface IUserService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterUserCommand cmd);
    Task<AuthResponseDto> LoginAsync(LoginUserCommand cmd);
    Task<UserProfileDto> GetByIdAsync(Guid id);
    Task<IEnumerable<UserProfileDto>> SearchAsync(string query);
}

public class UserAppService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserAppService> _logger;

    public UserAppService(IUserRepository repo, ITokenService tokenService, ILogger<UserAppService> logger)
    {
        _repo = repo;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterUserCommand cmd)
    {
        if (await _repo.GetByEmailAsync(cmd.Email) is not null)
            throw new ConflictException($"Email '{cmd.Email}' is already taken.");

        if (await _repo.GetByUsernameAsync(cmd.Username) is not null)
            throw new ConflictException($"Username '{cmd.Username}' is already taken.");

        var user = new User
        {
            Username = cmd.Username,
            Email = cmd.Email.ToLower(),
            PasswordHash = HashPassword(cmd.Password)
        };

        var created = await _repo.CreateAsync(user);
        _logger.LogInformation("User registered: {UserId} ({Username})", created.Id, created.Username);

        return new RegisterResponseDto
        {
            Id = created.Id,
            Username = created.Username,
            Email = created.Email
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserCommand cmd)
    {
        var user = await _repo.GetByEmailAsync(cmd.Email)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!VerifyPassword(cmd.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);
        _logger.LogInformation("User logged in: {UserId}", user.Id);

        return new AuthResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token
        };
    }

    public async Task<UserProfileDto> GetByIdAsync(Guid id)
    {
        var user = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<IEnumerable<UserProfileDto>> SearchAsync(string query)
    {
        var users = await _repo.SearchAsync(query);
        return users.Select(u => new UserProfileDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        });
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "cinegram-salt"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;
}