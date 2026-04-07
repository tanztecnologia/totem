using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(User user);
}

