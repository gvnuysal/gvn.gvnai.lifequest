using Gvn.GvnFramework.Domain.Entities;

namespace LifeQuest.Domain.Profiles;

public sealed class UserInterest : Entity
{
    public Guid UserProfileId { get; private set; }
    public Guid InterestId { get; private set; }

    /// <summary>0-1 arası ilgi ağırlığı.</summary>
    public double Weight { get; private set; }

    public InterestSource Source { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserInterest() { }

    internal UserInterest(Guid userProfileId, Guid interestId, double weight, InterestSource source, DateTime nowUtc)
    {
        UserProfileId = userProfileId;
        InterestId = interestId;
        Weight = Clamp(weight);
        Source = source;
        UpdatedAt = nowUtc;
    }

    internal void SetExplicit(double weight, DateTime nowUtc)
    {
        Weight = Clamp(weight);
        Source = InterestSource.Explicit;
        UpdatedAt = nowUtc;
    }

    internal void Adjust(double delta, DateTime nowUtc)
    {
        Weight = Clamp(Weight + delta);
        UpdatedAt = nowUtc;
    }

    private static double Clamp(double value) => Math.Round(Math.Clamp(value, 0d, 1d), 4);
}
