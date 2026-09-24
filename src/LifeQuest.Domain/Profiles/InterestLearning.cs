namespace LifeQuest.Domain.Profiles;

/// <summary>
/// Geri bildirim sinyallerinin ilgi ağırlıklarına etkisi. "Görevi tamamladı" otomatik olarak "çok sevdi"
/// anlamına gelmez; bu yüzden tamamlama zayıf, değerlendirme ve "daha fazla/az" tercihleri güçlü sinyaldir.
/// "Pahalı / zamanım yok / uzak" gibi skip sebepleri ilgi değil sürtünme (friction) bilgisidir ve ilgiyi düşürmez.
/// </summary>
public static class InterestLearning
{
    public const double CompletionDelta = 0.03;
    public const double NotInterestedDelta = -0.08;
    public const double RatingStep = 0.05;
    public const double MoreLikeThisDelta = 0.08;
    public const double LessLikeThisDelta = -0.10;

    /// <summary>Onboarding cold start kartı: "bana göre" / "bana göre değil".</summary>
    public const double StarterLikeDelta = 0.15;
    public const double StarterDislikeDelta = -0.10;

    /// <summary>Kullanıcının hiç seçmediği bir ilgi alanında olumlu sinyal gelirse bu ağırlıkla öğrenilir.</summary>
    public const double NewLearnedInterestBase = 0.30;

    /// <summary>1-5 değerlendirmeyi ağırlık değişimine çevirir: 5 → +0.10, 3 → 0, 1 → -0.10.</summary>
    public static double FromRating(int rating) => (rating - 3) * RatingStep;
}
