namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.AdminConfirmEmails;

using FluentValidation;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// An administrator vouches for the e-mail addresses of existing accounts, so they can sign in once
/// <c>Auth__RequireVerifiedEmail</c> is switched on (ADR 0010). For accounts that were verified out of band; it sends no mail.
/// </summary>
public record AdminConfirmEmailsCommand(IReadOnlyList<Guid> AccountIds) : ICommand<AdminConfirmEmailsResponse>;

public record AdminConfirmEmailsResponse(int Confirmed, int AlreadyConfirmed, int Skipped, int NotFound);

public sealed class AdminConfirmEmailsCommandValidator : AbstractValidator<AdminConfirmEmailsCommand>
{
    public const int MaxAccounts = 500;

    public AdminConfirmEmailsCommandValidator()
    {
        RuleFor(x => x.AccountIds)
            .NotNull().WithMessage("Account ids are required.")
            .Must(ids => ids is { Count: > 0 and <= MaxAccounts })
            .WithMessage($"Send between 1 and {MaxAccounts} account ids.");

        RuleForEach(x => x.AccountIds)
            .NotEqual(Guid.Empty).WithMessage("Account ids cannot be empty.");
    }
}

public sealed class AdminConfirmEmailsCommandHandler(
    IAccountRepository accountRepository,
    ICurrentUserService currentUser,
    ILogger<AdminConfirmEmailsCommandHandler> logger)
    : ICommandHandler<AdminConfirmEmailsCommand, AdminConfirmEmailsResponse>
{
    public async Task<Result<AdminConfirmEmailsResponse>> Handle(
        AdminConfirmEmailsCommand request,
        CancellationToken cancellationToken)
    {
        var wanted = request.AccountIds.Distinct().ToList();
        var accounts = await accountRepository.GetByIdsAsync(
            wanted.Select(id => new AccountId(id)).ToList(),
            cancellationToken);

        int confirmed = 0, already = 0, skipped = 0;
        foreach (var account in accounts)
        {
            if (account.IsEmailConfirmed)
            {
                already++;
            }
            else if (account.Email is null || account.ConfirmEmail().IsFailure)
            {
                skipped++; // no e-mail address to vouch for
            }
            else
            {
                confirmed++;
            }
        }

        // Who vouched for how many accounts. Ids are logged; e-mail addresses are not.
        logger.LogInformation(
            "Administrator {AdminId} confirmed the e-mail of {Confirmed} account(s) ({AlreadyConfirmed} already confirmed, {Skipped} skipped, {NotFound} not found).",
            currentUser.UserId, confirmed, already, skipped, wanted.Count - accounts.Count);

        return Result.Success(new AdminConfirmEmailsResponse(confirmed, already, skipped, wanted.Count - accounts.Count));
    }
}
