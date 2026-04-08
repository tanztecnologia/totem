using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Infrastructure;

public sealed class HttpTefPaymentService : ITefPaymentService
{
    public HttpTefPaymentService(HttpClient http, IOptions<TefApiOptions> options)
    {
        _http = http;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _http.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    private readonly HttpClient _http;
    private readonly TefApiOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<TefPixCharge> CreatePixChargeAsync(int amountCents, string reference, CancellationToken ct)
    {
        EnsureConfigured();
        if (amountCents <= 0) throw new ArgumentException("amountCents inválido.");
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("reference inválido.");

        var req = new CreatePixChargeRequest(amountCents, reference);
        var res = await PostAsync<CreatePixChargeRequest, TefPixChargeResponse>("/pix/charges", req, ct);
        return new TefPixCharge(res.Payload, res.ExpiresAt, res.ProviderReference);
    }

    public async Task<string> StartCardAsync(int amountCents, PaymentMethod method, string reference, CancellationToken ct)
    {
        EnsureConfigured();
        if (amountCents <= 0) throw new ArgumentException("amountCents inválido.");
        if (method is not PaymentMethod.CreditCard and not PaymentMethod.DebitCard) throw new ArgumentException("method inválido.");
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("reference inválido.");

        var req = new StartCardRequest(amountCents, method, reference);
        var res = await PostAsync<StartCardRequest, StartCardResponse>("/card/start", req, ct);
        return res.ProviderReference;
    }

    public async Task<TefPaymentConfirmation> ConfirmAsync(string providerReference, PaymentMethod method, CancellationToken ct)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(providerReference)) throw new ArgumentException("providerReference inválido.");

        var req = new ConfirmPaymentRequest(providerReference, method);
        var res = await PostAsync<ConfirmPaymentRequest, ConfirmPaymentResponse>("/payments/confirm", req, ct);
        return new TefPaymentConfirmation(res.IsApproved, res.TransactionId ?? string.Empty, res.Message);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("TEF BaseUrl não configurada.");
        }
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(path, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var msg = string.IsNullOrWhiteSpace(body) ? "Erro ao chamar TEF." : body;
            throw new InvalidOperationException(msg);
        }

        var parsed = JsonSerializer.Deserialize<TResponse>(body, JsonOptions);
        if (parsed is null) throw new InvalidOperationException("Resposta TEF inválida.");
        return parsed;
    }
}

public sealed record CreatePixChargeRequest(
    int AmountCents,
    string Reference
);

public sealed record TefPixChargeResponse(
    string Payload,
    DateTimeOffset ExpiresAt,
    string ProviderReference
);

public sealed record StartCardRequest(
    int AmountCents,
    PaymentMethod Method,
    string Reference
);

public sealed record StartCardResponse(
    string ProviderReference
);

public sealed record ConfirmPaymentRequest(
    string ProviderReference,
    PaymentMethod Method
);

public sealed record ConfirmPaymentResponse(
    bool IsApproved,
    string? TransactionId,
    string? Message
);

