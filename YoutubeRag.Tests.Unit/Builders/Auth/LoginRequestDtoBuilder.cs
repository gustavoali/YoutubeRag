using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Tests.Unit.Builders.Auth;

/// <summary>
/// Builder for creating LoginRequestDto test instances
/// </summary>
public class LoginRequestDtoBuilder
{
    private string _email = "test@example.com";
    private string _password = "Password123!";
    private bool _rememberMe = false;
    private string? _deviceInfo = null;

    public LoginRequestDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public LoginRequestDtoBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public LoginRequestDtoBuilder WithRememberMe(bool rememberMe = true)
    {
        _rememberMe = rememberMe;
        return this;
    }

    public LoginRequestDtoBuilder WithDeviceInfo(string deviceInfo)
    {
        _deviceInfo = deviceInfo;
        return this;
    }

    public LoginRequestDto Build()
    {
        return new LoginRequestDto
        {
            Email = _email,
            Password = _password,
            RememberMe = _rememberMe,
            DeviceInfo = _deviceInfo
        };
    }

    /// <summary>
    /// Creates a valid LoginRequestDto with default values
    /// </summary>
    /// <summary>
    /// Creates a valid LoginRequestDto with default values
    /// </summary>
    public static LoginRequestDto CreateValid() => new LoginRequestDtoBuilder().Build();

    /// <summary>
    /// Creates a LoginRequestDto with invalid email
    /// </summary>
    public static LoginRequestDto CreateWithInvalidEmail() =>
        new LoginRequestDtoBuilder().WithEmail("invalid-email").Build();

    /// <summary>
    /// Creates a LoginRequestDto with empty password
    /// </summary>
    public static LoginRequestDto CreateWithEmptyPassword() =>
        new LoginRequestDtoBuilder().WithPassword(string.Empty).Build();
}
