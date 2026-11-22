namespace OmniCore.Services.User.Application.Features.Addresses.RemoveMyAddress;

using FluentValidation;

public sealed class RemoveMyAddressCommandValidator : AbstractValidator<RemoveMyAddressCommand>
{
    public RemoveMyAddressCommandValidator() => RuleFor(x => x.AddressId).NotEmpty();
}
