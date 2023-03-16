using System;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Primitives;

namespace Austin.IdentityAwareProxy;

public class DefaultIapValidator : IIapValidator
{
    public async Task<IapPayload> Validate(StringValues jwtHeader, IEnumerable<string> trustedAudiences)
    {
        if (jwtHeader.Count != 1)
        {
            throw new InvalidJwtException($"Expected exactly one JWT header, got {jwtHeader.Count}");
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

        // TODO: consider enforcing the requirement for at least one audience here?

        return await JsonWebSignature.VerifySignedTokenAsync<IapPayload>(jwt, valSettings); ;
    }
}

