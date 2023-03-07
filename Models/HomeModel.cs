using SandwichTracker.Services;

namespace SandwichTracker.Models;

record class HomeModel(Dictionary<string, string?> Headers, IapPayload? JwtPayload, string ErrorMessage)
{
}