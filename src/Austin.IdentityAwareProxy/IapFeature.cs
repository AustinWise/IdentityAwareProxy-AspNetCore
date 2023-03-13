namespace Austin.IdentityAwareProxy;

internal class IapFeature : IIapFeature
{
    public IapFeature(IapPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        this.Payload = payload;
    }

    public IapPayload Payload { get; }
}
