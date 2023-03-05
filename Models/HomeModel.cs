using Google.Apis.Auth;

namespace SandwichTracker.Models;

public record class HomeModel(Dictionary<string, string?> Headers, JsonWebSignature.Payload? JwtPayload, string ErrorMessage)
{
}