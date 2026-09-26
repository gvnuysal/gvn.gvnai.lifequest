namespace LifeQuest.Application.Experiments;

/// <summary>Bir kullanıcının deney süresince bir gruptaki etkinliği.</summary>
public sealed record UserExperimentStats(
    Guid UserId,
    int Offered,
    int Accepted,
    int Completed,
    int Meaningful,
    int ExplorationOffered,
    int ExplorationAccepted,
    int NotInterestedSkips,
    int RatingSum,
    int RatingCount);

public sealed record VariantResult(
    int Users,
    int Offered,
    int Accepted,
    int Completed,
    int Meaningful,
    double NorthStar,
    double NorthStarStandardError,
    double AcceptanceRate,
    double CompletionRate,
    double ExplorationAcceptanceRate,
    double NotInterestedRate,
    double? AverageRating);

public sealed record NorthStarComparison(double Difference, double CiLow, double CiHigh, double? RelativeLift);

public enum ExperimentVerdict
{
    InsufficientData = 0,
    NoDifference = 1,
    TreatmentBetter = 2,
    TreatmentWorse = 3
}

/// <summary>
/// Deney sonuçları. Birincil metrik north-star: kullanıcı başına haftalık anlamlı deneyim. Kullanıcı birim olarak
/// alınır (aynı kullanıcının görevleri bağımsız değildir); fark için Welch yaklaşımıyla %95 güven aralığı hesaplanır.
/// </summary>
public static class ExperimentStatistics
{
    public const int MinUsersPerVariant = 30;

    /// <summary>Koruma metriği: Deneme grubunda "ilgimi çekmedi" oranı bu kadar puandan fazla artarsa uyarı verilir.</summary>
    public const double GuardrailMaxNotInterestedIncrease = 0.05;
    private const double Z95 = 1.959964;

    public static VariantResult Summarize(IReadOnlyCollection<UserExperimentStats> users, double weeks)
    {
        weeks = Math.Max(weeks, 1d / 7);
        var rates = users.Select(u => u.Meaningful / weeks).ToList();
        var (mean, se) = MeanAndStandardError(rates);
        int offered = users.Sum(u => u.Offered), accepted = users.Sum(u => u.Accepted), completed = users.Sum(u => u.Completed);
        int ratingCount = users.Sum(u => u.RatingCount);

        return new VariantResult(
            users.Count, offered, accepted, completed, users.Sum(u => u.Meaningful),
            Round(mean), Round(se),
            Rate(accepted, offered), Rate(completed, accepted),
            Rate(users.Sum(u => u.ExplorationAccepted), users.Sum(u => u.ExplorationOffered)),
            Rate(users.Sum(u => u.NotInterestedSkips), offered),
            ratingCount == 0 ? null : Math.Round(users.Sum(u => u.RatingSum) / (double)ratingCount, 2));
    }

    public static NorthStarComparison Compare(VariantResult control, VariantResult treatment)
    {
        var difference = treatment.NorthStar - control.NorthStar;
        var se = Math.Sqrt(control.NorthStarStandardError * control.NorthStarStandardError +
                           treatment.NorthStarStandardError * treatment.NorthStarStandardError);
        return new NorthStarComparison(
            Round(difference), Round(difference - Z95 * se), Round(difference + Z95 * se),
            control.NorthStar > 0 ? Math.Round(difference / control.NorthStar, 3) : null);
    }

    public static ExperimentVerdict Verdict(VariantResult control, VariantResult treatment, NorthStarComparison comparison)
    {
        if (control.Users < MinUsersPerVariant || treatment.Users < MinUsersPerVariant)
            return ExperimentVerdict.InsufficientData;
        if (comparison.CiLow > 0)
            return ExperimentVerdict.TreatmentBetter;
        if (comparison.CiHigh < 0)
            return ExperimentVerdict.TreatmentWorse;
        return ExperimentVerdict.NoDifference;
    }

    /// <summary>
    /// North-star artsa bile Deneme grubu önerileri belirgin daha sık "ilgimi çekmedi" diye geçiliyorsa ağırlık
    /// üretime alınmamalıdır. Yeterli veri yokken uyarı verilmez.
    /// </summary>
    public static bool GuardrailBreached(VariantResult control, VariantResult treatment)
        => control.Users >= MinUsersPerVariant && treatment.Users >= MinUsersPerVariant &&
           treatment.NotInterestedRate - control.NotInterestedRate > GuardrailMaxNotInterestedIncrease;

    private static (double Mean, double StandardError) MeanAndStandardError(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return (0, 0);

        var mean = values.Average();
        if (values.Count < 2)
            return (mean, 0);

        var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
        return (mean, Math.Sqrt(variance / values.Count));
    }

    private static double Rate(int part, int whole) => whole == 0 ? 0 : Math.Round(part / (double)whole, 3);

    private static double Round(double value) => Math.Round(value, 3);
}
