using Application.Auth.Commands.ConfirmEmail;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenProvider _verificationTokenProvider;

    public ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenProvider verificationTokenProvider)
    {
        _userRepository = userRepository;
        _verificationTokenProvider = verificationTokenProvider;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = await _verificationTokenProvider.ValidateTokenAsync(request.Token, cancellationToken);

        if (userId is null)
        {
            throw new UnauthorizedException("Invalid or expired email verification token.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || user.Id != userId)
        {
            throw new UnauthorizedException("Invalid or expired email verification token.");
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        user.ConfirmEmail();
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
