using YoutubeRag.Application.DTOs.Auth;

namespace YoutubeRag.Tests.Unit.Builders.Auth;

/// <summary>
/// Builder for creating ChangePasswordRequestDto test instances
/// Handles positional record constructor
/// </summary>
public class ChangePasswordRequestDtoBuilder
{
    private string _currentPassword = "OldPassword123!";
    private string _newPassword = "NewPassword123!";

    public ChangePasswordRequestDtoBuilder WithCurrentPassword(string currentPassword)
    {
        _currentPassword = currentPassword;
        return this;
    }

    public ChangePasswordRequestDtoBuilder WithNewPassword(string newPassword)
    {
        _newPassword = newPassword;
        return this;
    }

    public ChangePasswordRequestDto Build()
    {
        // Positional record constructor
        return new ChangePasswordRequestDto(_currentPassword, _newPassword);
    }

    /// <summary>
    /// Creates a valid ChangePasswordRequestDto with default values
    /// </summary>
    public static ChangePasswordRequestDto CreateValid() => new ChangePasswordRequestDtoBuilder().Build();

    /// <summary>
    /// Creates a ChangePasswordRequestDto with empty current password
    /// </summary>
    public static ChangePasswordRequestDto CreateWithEmptyCurrentPassword() =>
        new ChangePasswordRequestDtoBuilder().WithCurrentPassword(string.Empty).Build();

    /// <summary>
    /// Creates a ChangePasswordRequestDto with same passwords
    /// </summary>
    public static ChangePasswordRequestDto CreateWithSamePasswords() =>
        new ChangePasswordRequestDtoBuilder()
            .WithCurrentPassword("Password123!")
            .WithNewPassword("Password123!")
            .Build();

    /// <summary>
    /// Creates a ChangePasswordRequestDto with weak new password
    /// </summary>
    public static ChangePasswordRequestDto CreateWithWeakNewPassword() =>
        new ChangePasswordRequestDtoBuilder()
            .WithNewPassword("weak")
            .Build();
}
