
using System.Globalization;
using System.Text.RegularExpressions;

namespace Austin.IdentityAwareProxy;

public partial class IapAccessLevel
{
    [GeneratedRegex(@"^accessPolicies/(?<policy>\d+)/accessLevels/(?<level>.+)$", RegexOptions.ExplicitCapture)]
    private static partial Regex AccessLevelRegex();

    public static IapAccessLevel Parse(string level)
    {
        var regex = AccessLevelRegex();
        Match m = regex.Match(level);

        if (!m.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid access level format.");
        }

        long policyId = long.Parse(m.Groups["policy"].Value, CultureInfo.InvariantCulture);
        string levelName = m.Groups["level"].Value;

        return new IapAccessLevel(policyId, levelName);
    }

    public IapAccessLevel(long policyId, string level)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(level);

        this.PolicyId = policyId;
        this.Level = level;
    }

    public long PolicyId { get; }

    public string Level { get; }
}