using Google.Apis.Auth;
using Microsoft.Extensions.Primitives;

namespace Austin.IdentityAwareProxy;

public interface IIapValidator
{
    /// <exception cref="InvalidJwtException">Thrown if the JWT is not valid.</exception>
    Task<IapPayload> Validate(StringValues jwtHeader, string[] trustedAudiences, CancellationToken ct);
}
