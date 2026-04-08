using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Infrastructure;

public sealed class FakeTefPaymentService : ITefPaymentService
{
    public Task<TefPixCharge> CreatePixChargeAsync(int amountCents, string reference, CancellationToken ct)
    {
        if (amountCents <= 0) throw new ArgumentException("amountCents inválido.");
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(5);
        var providerReference = $"pix-{Guid.NewGuid():N}";
        var payload = $"000201|AMOUNT={amountCents}|REF={reference}|EXPIRES={expiresAt:O}|PR={providerReference}";
        return Task.FromResult(new TefPixCharge(payload, expiresAt, providerReference));
    }

    public Task<string> StartCardAsync(int amountCents, PaymentMethod method, string reference, CancellationToken ct)
    {
        if (amountCents <= 0) throw new ArgumentException("amountCents inválido.");
        if (method is not PaymentMethod.CreditCard and not PaymentMethod.DebitCard) throw new ArgumentException("method inválido.");
        var providerReference = $"card-{Guid.NewGuid():N}";
        return Task.FromResult(providerReference);
    }

    public Task<TefPaymentConfirmation> ConfirmAsync(string providerReference, PaymentMethod method, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerReference)) throw new ArgumentException("providerReference inválido.");
        var transactionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        return Task.FromResult(new TefPaymentConfirmation(true, transactionId, "APROVADO"));
    }
}

