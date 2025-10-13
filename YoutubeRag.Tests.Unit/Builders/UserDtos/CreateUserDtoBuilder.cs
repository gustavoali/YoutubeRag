using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Tests.Unit.Builders.UserDtos;

/// <summary>
/// Builder for creating CreateUserDto test instances
/// </summary>
public class CreateUserDtoBuilder
{
    private string _name = "Test User";
    private string _email = "testuser@example.com";
    private string _password = "Password123!";
    private string _confirmPassword = "Password123!";
    private string? _bio = null;
    private string? _avatar = null;

    public CreateUserDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateUserDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CreateUserDtoBuilder WithPassword(string password)
    {
        _password = password;
        _confirmPassword = password; // Auto-match by default
        return this;
    }

    public CreateUserDtoBuilder WithConfirmPassword(string confirmPassword)
    {
        _confirmPassword = confirmPassword;
        return this;
    }

    public CreateUserDtoBuilder WithBio(string bio)
    {
        _bio = bio;
        return this;
    }

    public CreateUserDtoBuilder WithAvatar(string avatar)
    {
        _avatar = avatar;
        return this;
    }

    public CreateUserDto Build()
    {
        return new CreateUserDto
        {
            Name = _name,
            Email = _email,
            Password = _password,
            ConfirmPassword = _confirmPassword,
            Bio = _bio,
            Avatar = _avatar
        };
    }

    public static CreateUserDto CreateValid() => new CreateUserDtoBuilder().Build();

    public static CreateUserDto CreateWithMismatchedPasswords() =>
        new CreateUserDtoBuilder()
            .WithPassword("Password123!")
            .WithConfirmPassword("DifferentPassword123!")
            .Build();

    public static CreateUserDto CreateWithWeakPassword() =>
        new CreateUserDtoBuilder()
            .WithPassword("weak")
            .Build();
}
