using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Primitives;

namespace Austin.IdentityAwareProxy;

public class DefaultIapValidator : IIapValidator
{
    public async Task<IapPayload> Validate(StringValues jwtHeader, string[] trustedAudiences, CancellationToken ct)
    {
        if (jwtHeader.Count != 1)
        {
            throw new InvalidJwtException($"Expected exactly one JWT header, got {jwtHeader.Count}");
        }
        if (trustedAudiences.Length == 0)
        {
            throw new InvalidOperationException("Expected 1 or more trusted audiences.");
        }

        string? jwt = jwtHeader[0];

        var valSettings = new SignedTokenVerificationOptions()
        {
            IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
            ExpiryClockTolerance = TimeSpan.FromMinutes(30),
            CertificatesUrl = GoogleAuthConsts.IapKeySetUrl,
            TrustedIssuers = { "https://cloud.google.com/iap" },
        };
        foreach (var aud in trustedAudiences)
        {
            valSettings.TrustedAudiences.Add(aud);
        }

        return await JsonWebSignature.VerifySignedTokenAsync<IapPayload>(jwt, valSettings, ct);
    }
}

