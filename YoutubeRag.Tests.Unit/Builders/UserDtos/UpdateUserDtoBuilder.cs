using YoutubeRag.Application.DTOs.User;

namespace YoutubeRag.Tests.Unit.Builders.UserDtos;

public class UpdateUserDtoBuilder
{
    private string? _name = null;
    private string? _bio = null;
    private string? _avatar = null;
    private bool? _removeAvatar = null;
    private bool? _isActive = null;
    private string? _email = null;
    private bool? _isEmailVerified = null;

    public UpdateUserDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UpdateUserDtoBuilder WithBio(string bio)
    {
        _bio = bio;
        return this;
    }

    public UpdateUserDtoBuilder WithAvatar(string avatar)
    {
        _avatar = avatar;
        return this;
    }

    public UpdateUserDtoBuilder WithRemoveAvatar(bool remove = true)
    {
        _removeAvatar = remove;
        return this;
    }

    public UpdateUserDtoBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public UpdateUserDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UpdateUserDtoBuilder WithIsEmailVerified(bool verified)
    {
        _isEmailVerified = verified;
        return this;
    }

    public UpdateUserDto Build()
    {
        return new UpdateUserDto
        {
            Name = _name,
            Bio = _bio,
            Avatar = _avatar,
            RemoveAvatar = _removeAvatar,
            IsActive = _isActive,
            Email = _email,
            IsEmailVerified = _isEmailVerified
        };
    }

    public static UpdateUserDto CreateWithNameUpdate() =>
        new UpdateUserDtoBuilder().WithName("Updated Name").Build();

    public static UpdateUserDto CreateWithEmailUpdate(string newEmail) =>
        new UpdateUserDtoBuilder().WithEmail(newEmail).Build();

    public static UpdateUserDto CreateEmpty() =>
        new UpdateUserDtoBuilder().Build();
}
