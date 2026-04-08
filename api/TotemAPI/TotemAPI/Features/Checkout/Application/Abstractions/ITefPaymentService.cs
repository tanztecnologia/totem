using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Application.Abstractions;

public sealed record TefPixCharge(
    string Payload,
    DateTimeOffset ExpiresAt,
    string ProviderReference
);

public sealed record TefPaymentConfirmation(
    bool IsApproved,
    string TransactionId,
    string? Message
);

public interface ITefPaymentService
{
    Task<TefPixCharge> CreatePixChargeAsync(int amountCents, string reference, CancellationToken ct);
    Task<string> StartCardAsync(int amountCents, PaymentMethod method, string reference, CancellationToken ct);
    Task<TefPaymentConfirmation> ConfirmAsync(string providerReference, PaymentMethod method, CancellationToken ct);
}

