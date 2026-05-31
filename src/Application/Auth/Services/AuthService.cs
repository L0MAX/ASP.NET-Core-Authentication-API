using Application.Auth.DTOs.Requests;
using Application.Auth.Commands.ForgotPassword;
using Application.Auth.Commands.Login;
using Application.Auth.Commands.RefreshToken;
using Application.Auth.Commands.Register;
using Application.Auth.Commands.ResetPassword;
using Application.Auth.DTOs.Responses;
using Application.Auth.Queries.GetUserById;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IMediator _mediator;

    public AuthService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        _mediator.Send(
            new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password),
            cancellationToken);

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

    public Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
        _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        _mediator.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);

    public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        _mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

    public Task<UserResponse> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
}
