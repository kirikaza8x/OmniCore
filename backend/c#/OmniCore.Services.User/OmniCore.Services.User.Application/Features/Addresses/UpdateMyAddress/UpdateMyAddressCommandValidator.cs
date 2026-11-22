namespace OmniCore.Services.User.Application.Features.Addresses.UpdateMyAddress;

using FluentValidation;

public sealed class UpdateMyAddressCommandValidator : AbstractValidator<UpdateMyAddressCommand>
{
    public UpdateMyAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AddressLine2).MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(50);
        RuleFor(x => x.State).MaximumLength(50);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ZipCode).MaximumLength(20);
    }
}
