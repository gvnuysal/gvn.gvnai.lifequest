using System.Text;
using LifeQuest.Domain.Common;

namespace LifeQuest.Application.Narration;

/// <summary>
/// LLM adaptörleri için prompt. Yalnızca <see cref="NarrationRequest"/> alanlarını kullanır; model yalnızca
/// başlık ve anlatımı yeniden yazar. Güvenlik, XP, süre ve maliyet kuralları backend'dedir ve modele
/// "değiştirme" diye bırakılmaz, çıktı <see cref="NarrationGuard"/> ile ayrıca doğrulanır.
/// </summary>
public static class NarrationPromptBuilder
{
    public const string SystemPrompt =
        "Sen LifeQuest için quest anlatımı yazan bir yardımcısın. Verilen quest'i Türkçe, sıcak ve sakin bir tonla " +
        "yeniden yaz. Kurallar: başlık en fazla 80, açıklama en fazla 300 karakter; yeni sayı, süre, fiyat, para birimi, " +
        "XP veya bağlantı ekleme; riskli, yasa dışı, alkol içeren veya kişiyi gece ıssız yerlere yönlendiren içerik " +
        "yazma; konum, fotoğraf ya da kişisel bilgi paylaşmayı isteme; quest'in kategorisini ve özünü değiştirme. " +
        "Yalnızca JSON döndür: {\"title\": \"...\", \"description\": \"...\"}";

    public static string BuildUserPrompt(NarrationRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Kategori: {request.Category.DisplayName()}");
        sb.AppendLine($"Konular: {string.Join(", ", request.TopicNames)}");
        sb.AppendLine($"Başlık: {request.BaseTitle}");
        sb.AppendLine($"Açıklama: {request.BaseDescription}");
        return sb.ToString();
    }
}
