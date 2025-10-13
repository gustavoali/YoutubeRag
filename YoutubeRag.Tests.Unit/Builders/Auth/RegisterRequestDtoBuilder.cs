using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Tests.Unit.Builders.Auth;

/// <summary>
/// Builder for creating RegisterRequestDto test instances
/// </summary>
public class RegisterRequestDtoBuilder
{
    private string _name = "Test User";
    private string _email = "test@example.com";
    private string _password = "Password123!";
    private string _confirmPassword = "Password123!";
    private bool _acceptTerms = true;
    private bool _subscribeToNewsletter = false;
    private string? _deviceInfo = null;

    public RegisterRequestDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RegisterRequestDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public RegisterRequestDtoBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public RegisterRequestDtoBuilder WithConfirmPassword(string confirmPassword)
    {
        _confirmPassword = confirmPassword;
        return this;
    }

    public RegisterRequestDtoBuilder WithAcceptTerms(bool acceptTerms)
    {
        _acceptTerms = acceptTerms;
        return this;
    }

    public RegisterRequestDtoBuilder WithSubscribeToNewsletter(bool subscribe = true)
    {
        _subscribeToNewsletter = subscribe;
        return this;
    }

    public RegisterRequestDtoBuilder WithDeviceInfo(string deviceInfo)
    {
        _deviceInfo = deviceInfo;
        return this;
    }

    public RegisterRequestDto Build()
    {
        return new RegisterRequestDto
        {
            Name = _name,
            Email = _email,
            Password = _password,
            ConfirmPassword = _confirmPassword,
            AcceptTerms = _acceptTerms,
            SubscribeToNewsletter = _subscribeToNewsletter,
            DeviceInfo = _deviceInfo
        };
    }

    /// <summary>
    /// Creates a valid RegisterRequestDto with default values
    /// </summary>
    public static RegisterRequestDto CreateValid() => new RegisterRequestDtoBuilder().Build();

    /// <summary>
    /// Creates a RegisterRequestDto with mismatched passwords
    /// </summary>
    public static RegisterRequestDto CreateWithMismatchedPasswords() =>
        new RegisterRequestDtoBuilder()
            .WithPassword("Password123!")
            .WithConfirmPassword("DifferentPassword123!")
            .Build();

    /// <summary>
    /// Creates a RegisterRequestDto with weak password
    /// </summary>
    public static RegisterRequestDto CreateWithWeakPassword() =>
        new RegisterRequestDtoBuilder()
            .WithPassword("weak")
            .WithConfirmPassword("weak")
            .Build();

    /// <summary>
    /// Creates a RegisterRequestDto without accepting terms
    /// </summary>
    public static RegisterRequestDto CreateWithoutAcceptingTerms() =>
        new RegisterRequestDtoBuilder().WithAcceptTerms(false).Build();

    /// <summary>
    /// Creates a RegisterRequestDto with invalid email
    /// </summary>
    public static RegisterRequestDto CreateWithInvalidEmail() =>
        new RegisterRequestDtoBuilder().WithEmail("invalid-email").Build();
}
