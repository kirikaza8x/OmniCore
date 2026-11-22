namespace OmniCore.Services.User.Application.Features.Addresses.AddMyAddress;

using FluentValidation;

public sealed class AddMyAddressCommandValidator : AbstractValidator<AddMyAddressCommand>
{
    public AddMyAddressCommandValidator()
    {
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AddressLine2).MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(50);
        RuleFor(x => x.State).MaximumLength(50);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ZipCode).MaximumLength(20);
    }
}
