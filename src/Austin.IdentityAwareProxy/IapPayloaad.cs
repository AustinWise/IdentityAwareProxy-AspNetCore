
using Google.Apis.Auth;
using Newtonsoft.Json;

namespace Austin.IdentityAwareProxy;

class GoogleInfo
{
    [JsonProperty("access_levels")]
    public List<string>? AccessLevels { get; set; }
}

class IapPayload : JsonWebSignature.Payload
{
    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("hd")]
    public string? HostedDomain { get; set; }

    [JsonProperty("google")]
    public GoogleInfo? GoogleInfo { get; set; }
}