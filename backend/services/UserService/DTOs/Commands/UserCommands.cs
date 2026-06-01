using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs.Commands;

public class RegisterUserCommand
{
    [Required]
    [MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class LoginUserCommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserCommand
{
    [MinLength(3), MaxLength(50)]
    public string? Username { get; set; }
}

public class PromoteUserCommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string AdminSecret { get; set; } = string.Empty;
}
