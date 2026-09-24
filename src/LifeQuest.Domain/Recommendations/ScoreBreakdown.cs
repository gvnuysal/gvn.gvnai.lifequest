namespace LifeQuest.Domain.Recommendations;

/// <summary>
/// QuestScore = Interest + Novelty + Context + GoalFit + Diversity + FeedbackFit - Repetition - Friction - Risk.
/// Bileşenler 0-1 normalize, <see cref="Total"/> ağırlıklı toplamdır. Quest snapshot'ında saklanır; hem
/// "neden bunu önerdim?" açıklaması hem de offline öneri kalitesi analizi için kullanılır.
/// </summary>
public sealed record ScoreBreakdown(
    double Interest,
    double Novelty,
    double Context,
    double GoalFit,
    double Diversity,
    double FeedbackFit,
    double Repetition,
    double Friction,
    double Risk,
    double Total)
{
    public static readonly ScoreBreakdown Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
