using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using static LifeQuest.Domain.Common.DayPart;

namespace LifeQuest.Infrastructure.Persistence.Seed;

/// <summary>
/// MVP başlangıç kataloğu. Tüm template'ler editoryal olarak güvenli (Safe), 18+ ve fotoğraf/konum
/// doğrulaması gerektirmeyecek şekilde yazılmıştır. Analizdeki hedef olan ilk 100 template için genişletilmelidir.
/// </summary>
internal static class CatalogSeedData
{
    public sealed record InterestSeed(string Code, string Name, LifeCategory Category);

    public sealed record RelationSeed(string From, string To, double Weight, InterestRelationType Type = InterestRelationType.Adjacent);

    public sealed record TemplateSeed(
        string Code, string Title, string Description,
        QuestType Type, Difficulty Difficulty, LifeCategory Category, LifeCategory? Secondary,
        int MinMinutes, int MaxMinutes, CostBand Cost, DayPart DayParts,
        bool RequiresCity, bool IsOutdoor, int CooldownDays, double Risk, string[] Interests);

    public static readonly InterestSeed[] Interests =
    [
        new("coffee", "Kahve", LifeCategory.Explorer),
        new("cafe-culture", "Kafe Kültürü", LifeCategory.Explorer),
        new("neighborhoods", "Semt Keşfi", LifeCategory.Explorer),
        new("street-food", "Sokak Lezzetleri", LifeCategory.Explorer),
        new("nature", "Doğa", LifeCategory.Explorer),
        new("local-markets", "Yerel Pazarlar", LifeCategory.Explorer),

        new("museums", "Müzeler", LifeCategory.Culture),
        new("art", "Sanat", LifeCategory.Culture),
        new("cinema", "Sinema", LifeCategory.Culture),
        new("theatre", "Tiyatro", LifeCategory.Culture),
        new("architecture", "Mimari", LifeCategory.Culture),
        new("history", "Tarih", LifeCategory.Culture),
        new("live-music", "Canlı Müzik", LifeCategory.Culture),

        new("reading", "Okuma", LifeCategory.Learning),
        new("languages", "Yabancı Dil", LifeCategory.Learning),
        new("science", "Bilim", LifeCategory.Learning),
        new("podcasts", "Podcast", LifeCategory.Learning),
        new("cooking", "Yemek Yapma", LifeCategory.Learning),

        new("friends", "Arkadaşlar", LifeCategory.Social),
        new("volunteering", "Gönüllülük", LifeCategory.Social),
        new("board-games", "Kutu Oyunları", LifeCategory.Social),
        new("community-events", "Topluluk Etkinlikleri", LifeCategory.Social),

        new("walking", "Yürüyüş", LifeCategory.Fitness),
        new("running", "Koşu", LifeCategory.Fitness),
        new("cycling", "Bisiklet", LifeCategory.Fitness),
        new("yoga", "Yoga", LifeCategory.Fitness),
        new("swimming", "Yüzme", LifeCategory.Fitness),

        new("photography", "Fotoğrafçılık", LifeCategory.Creativity),
        new("street-photography", "Sokak Fotoğrafçılığı", LifeCategory.Creativity),
        new("drawing", "Çizim", LifeCategory.Creativity),
        new("writing", "Yazarlık", LifeCategory.Creativity),
        new("music-making", "Müzik Yapma", LifeCategory.Creativity),
        new("crafts", "El Sanatları", LifeCategory.Creativity)
    ];

    /// <summary>Taste Graph: Kahve → Kafe Kültürü → Mimari → Fotoğrafçılık → Sokak Fotoğrafçılığı gibi geçişler.</summary>
    public static readonly RelationSeed[] Relations =
    [
        new("coffee", "cafe-culture", 0.9),
        new("cafe-culture", "architecture", 0.5),
        new("architecture", "photography", 0.6),
        new("photography", "street-photography", 0.8, InterestRelationType.Specialization),
        new("neighborhoods", "architecture", 0.6),
        new("neighborhoods", "street-photography", 0.5),
        new("cinema", "theatre", 0.6),
        new("cinema", "writing", 0.4),
        new("art", "museums", 0.7),
        new("art", "drawing", 0.6),
        new("museums", "history", 0.7),
        new("history", "architecture", 0.6),
        new("reading", "writing", 0.6),
        new("reading", "podcasts", 0.5),
        new("cooking", "street-food", 0.5),
        new("street-food", "local-markets", 0.7),
        new("walking", "nature", 0.7),
        new("walking", "photography", 0.4),
        new("running", "walking", 0.6),
        new("live-music", "music-making", 0.5),
        new("board-games", "friends", 0.6),
        new("volunteering", "community-events", 0.7),
        new("languages", "podcasts", 0.4),
        new("science", "museums", 0.4),
        new("yoga", "walking", 0.3),
        new("cycling", "nature", 0.6),
        new("crafts", "drawing", 0.5)
    ];

    private const DayPart Daytime = Morning | Afternoon;
    private const DayPart Waking = Morning | Afternoon | Evening;

    public static readonly TemplateSeed[] Templates =
    [
        // ── Explorer ─────────────────────────────────────────────────────────
        new("explorer-new-street", "Hiç yürümediğin bir sokaktan geç",
            "Bugün alışık olduğun rotadan bir sokak sap ve dikkatini çeken üç detayı not et.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Explorer, LifeCategory.Fitness,
            10, 20, CostBand.Free, Waking, false, true, 3, 0, ["neighborhoods", "walking"]),
        new("explorer-new-cafe", "Yeni bir kafe keşfet",
            "Daha önce hiç gitmediğin bir kafeye git ve menüde ilk kez denediğin bir şey sipariş et.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Explorer, null,
            45, 90, CostBand.Low, Waking, true, false, 7, 0, ["coffee", "cafe-culture"]),
        new("explorer-local-market", "Semt pazarını gez",
            "Bir semt pazarını dolaş ve daha önce tatmadığın bir ürünü dene.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Explorer, null,
            60, 120, CostBand.Low, Daytime, true, true, 14, 0, ["local-markets", "street-food"]),
        new("explorer-neighborhood", "Hiç gitmediğin bir semti keşfet",
            "Şehrinde hiç vakit geçirmediğin bir semte git; bir meydan, bir yapı ve bir mekân keşfet.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Explorer, LifeCategory.Culture,
            90, 180, CostBand.Low, Daytime, true, true, 14, 0, ["neighborhoods", "architecture"]),
        new("explorer-street-food", "Yeni bir sokak lezzeti tat",
            "Daha önce denemediğin yöresel bir sokak lezzetini bul ve tat.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Explorer, null,
            45, 90, CostBand.Low, Afternoon | Evening, true, false, 7, 0, ["street-food"]),
        new("explorer-sunrise", "Gün doğumunu açık havada izle",
            "Erken kalk, güvenli ve bildiğin bir noktadan gün doğumunu izle.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Explorer, LifeCategory.Creativity,
            30, 60, CostBand.Free, Morning, false, true, 14, 0, ["nature", "photography"]),
        new("explorer-nature-day", "Şehir yakınında bir doğa rotası",
            "Şehrin yakınındaki işaretli ve popüler bir doğa rotasında gün geçir. Hava durumunu kontrol et, yanına su al.",
            QuestType.Adventure, Difficulty.Medium, LifeCategory.Explorer, LifeCategory.Fitness,
            180, 360, CostBand.Low, Morning, true, true, 30, 0.15, ["nature", "walking"]),

        // ── Culture ──────────────────────────────────────────────────────────
        new("culture-new-artist", "Yeni bir sanatçının üç eserini keşfet",
            "Adını ilk kez duyduğun bir sanatçının üç eserine (resim, şarkı ya da film) göz at.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Culture, LifeCategory.Learning,
            10, 20, CostBand.Free, DayPart.Any, false, false, 2, 0, ["art", "live-music"]),
        new("culture-new-venue", "Yeni bir kültür mekânını keşfet",
            "Şehrinde daha önce gitmediğin bir kültür mekânını (müze, galeri, kültür merkezi) ziyaret et.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Culture, LifeCategory.Explorer,
            60, 120, CostBand.Low, Waking, true, false, 14, 0, ["museums", "art", "history"]),
        new("culture-photo-exhibition", "Bir fotoğraf sergisini gez",
            "Bir fotoğraf sergisini gez ve seni en çok etkileyen karenin neden etkilediğini not et.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Culture, LifeCategory.Creativity,
            60, 120, CostBand.Low, Waking, true, false, 14, 0, ["art", "photography"]),
        new("culture-short-film", "Bir kısa film gösterimine katıl",
            "Bir kısa film gösterimine ya da festival seçkisine katıl.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Culture, null,
            60, 120, CostBand.Low, Evening, true, false, 14, 0, ["cinema"]),
        new("culture-classic-film", "Hiç izlemediğin bir klasik filmi izle",
            "Sinema tarihinden hiç izlemediğin bir klasiği seç ve izle.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Culture, LifeCategory.Learning,
            90, 150, CostBand.Free, Evening | Night, false, false, 7, 0, ["cinema"]),
        new("culture-theatre", "Bir tiyatro oyununa git",
            "Daha önce izlemediğin bir tiyatro oyununa bilet al ve izle.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Culture, null,
            120, 180, CostBand.Medium, Evening, true, false, 21, 0, ["theatre"]),
        new("culture-live-music", "Yeni bir türde canlı müzik dinle",
            "Normalde dinlemediğin bir türde canlı müzik performansına git.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Culture, LifeCategory.Social,
            90, 180, CostBand.Medium, Evening | Night, true, false, 14, 0, ["live-music"]),
        new("culture-architecture-story", "Tarihi bir yapının hikâyesini yerinde öğren",
            "Şehrindeki tarihi bir yapıyı ziyaret et ve hikâyesini yerinde öğren.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Culture, LifeCategory.Explorer,
            60, 120, CostBand.Free, Daytime, true, true, 14, 0, ["architecture", "history"]),

        // ── Learning ─────────────────────────────────────────────────────────
        new("learning-ten-pages", "Yeni bir kitaptan 10 sayfa oku",
            "Başlamadığın bir kitabı aç ve ilk 10 sayfasını oku.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Learning, null,
            15, 20, CostBand.Free, DayPart.Any, false, false, 1, 0, ["reading"]),
        new("learning-podcast-walk", "Yürürken yeni bir konuda podcast dinle",
            "Hiç bilmediğin bir konuda bir podcast bölümü seç ve kısa bir yürüyüşte dinle.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Learning, LifeCategory.Fitness,
            15, 30, CostBand.Free, Waking, false, true, 2, 0, ["podcasts", "walking"]),
        new("learning-ten-words", "Öğrenmek istediğin dilde 10 kelime",
            "Öğrenmek istediğin dilde 10 yeni kelime öğren ve her biriyle bir cümle kur.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Learning, null,
            10, 15, CostBand.Free, DayPart.Any, false, false, 1, 0, ["languages"]),
        new("learning-new-recipe", "Hiç yapmadığın bir yemeği pişir",
            "Daha önce hiç yapmadığın bir yemeği tarifinden pişir.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Learning, LifeCategory.Creativity,
            60, 120, CostBand.Low, Evening, false, false, 7, 0.05, ["cooking"]),
        new("learning-workshop", "Bir atölye ya da açık derse katıl",
            "Merak ettiğin bir konuda atölyeye, söyleşiye ya da açık derse katıl.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Learning, LifeCategory.Social,
            90, 180, CostBand.Medium, Afternoon | Evening, true, false, 21, 0, ["community-events", "science"]),
        new("learning-science-center", "Bir bilim merkezini ziyaret et",
            "Bir bilim merkezi, planetaryum ya da bilim müzesini ziyaret et.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Learning, LifeCategory.Culture,
            90, 150, CostBand.Low, Daytime, true, false, 30, 0, ["science", "museums"]),
        new("learning-mini-course", "Kısa bir online kursu bitir",
            "Merak ettiğin bir konuda kısa bir online kursu baştan sona tamamla.",
            QuestType.Epic, Difficulty.Hard, LifeCategory.Learning, null,
            300, 900, CostBand.Free, DayPart.Any, false, false, 60, 0, ["science", "languages"]),

        // ── Social ───────────────────────────────────────────────────────────
        new("social-reconnect", "Uzun zamandır konuşmadığın birine yaz",
            "Uzun süredir konuşmadığın bir arkadaşına nasıl olduğunu soran bir mesaj gönder.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Social, null,
            5, 15, CostBand.Free, Waking, false, false, 3, 0, ["friends"]),
        new("social-board-game-night", "Kutu oyunu akşamı düzenle",
            "Arkadaşlarınla daha önce oynamadığınız bir kutu oyununu deneyin.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Social, null,
            120, 180, CostBand.Low, Evening, false, false, 14, 0, ["board-games", "friends"]),
        new("social-volunteer", "Bir gönüllülük etkinliğine katıl",
            "Güvenilir bir kurumun düzenlediği bir gönüllülük etkinliğine katıl.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Social, null,
            120, 240, CostBand.Free, Daytime, true, false, 21, 0, ["volunteering", "community-events"]),
        new("social-community-meetup", "Yerel bir topluluk buluşmasına katıl",
            "İlgi alanlarından birinde yerel bir topluluk buluşmasına katıl.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Social, null,
            60, 150, CostBand.Low, Evening, true, false, 14, 0, ["community-events"]),
        new("social-cook-together", "Bir arkadaşınla birlikte yemek yapın",
            "Bir arkadaşınla ikinizin de ilk kez deneyeceği bir tarifi birlikte pişirin.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Social, LifeCategory.Learning,
            90, 180, CostBand.Low, Evening, false, false, 14, 0.05, ["cooking", "friends"]),

        // ── Fitness ──────────────────────────────────────────────────────────
        new("fitness-stretch", "10 dakikalık esneme rutini",
            "Kendini zorlamadan 10 dakikalık bir esneme rutini yap.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Fitness, null,
            10, 15, CostBand.Free, DayPart.Any, false, false, 1, 0, ["yoga"]),
        new("fitness-walk-photo", "45 dakikalık yürüyüşte 5 ilginç kare",
            "45 dakikalık bir yürüyüşe çık ve yol boyunca dikkatini çeken 5 kare yakala.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Fitness, LifeCategory.Creativity,
            45, 60, CostBand.Free, Waking, false, true, 3, 0, ["walking", "photography"]),
        new("fitness-new-route-run", "Yeni bir rotada hafif tempolu koş",
            "Bildiğin güvenli bir bölgede yeni bir rota seç ve konuşabileceğin tempoda koş.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Fitness, LifeCategory.Explorer,
            30, 60, CostBand.Free, Morning | Evening, false, true, 7, 0.1, ["running"]),
        new("fitness-bike-tour", "Bisikletle sahil ya da park rotası",
            "Bisiklet yolu olan bir sahil ya da park rotasını keşfet. Kask tak.",
            QuestType.Adventure, Difficulty.Medium, LifeCategory.Fitness, LifeCategory.Explorer,
            120, 240, CostBand.Low, Daytime, true, true, 30, 0.2, ["cycling", "nature"]),
        new("fitness-yoga-class", "Bir deneme yoga dersine katıl",
            "Başlangıç seviyesine uygun bir deneme yoga dersine katıl.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Fitness, null,
            60, 90, CostBand.Medium, Morning | Evening, true, false, 30, 0, ["yoga"]),
        new("fitness-swim", "Kapalı bir havuzda yüz",
            "Cankurtaranı olan kapalı bir havuzda kendi temponda yüz.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Fitness, null,
            60, 90, CostBand.Medium, Waking, true, false, 14, 0.1, ["swimming"]),

        // ── Creativity ───────────────────────────────────────────────────────
        new("creativity-photo-theme", "Tek temada 5 fotoğraf",
            "Bugün bir tema seç (gölge, kırmızı, yuvarlak…) ve bu temada 5 fotoğraf çek.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Creativity, null,
            15, 20, CostBand.Free, Waking, false, false, 2, 0, ["photography"]),
        new("creativity-sketch", "15 dakikada bir nesne çiz",
            "Etrafındaki bir nesneyi seç ve 15 dakikada çiz. Güzel olması gerekmiyor.",
            QuestType.Daily, Difficulty.Easy, LifeCategory.Creativity, null,
            15, 20, CostBand.Free, DayPart.Any, false, false, 2, 0, ["drawing"]),
        new("creativity-street-photo", "Sokak fotoğrafçılığı turu",
            "Kalabalık ve güvenli bir caddede sokak fotoğrafçılığı turu yap; insanların rızasına saygı göster.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Creativity, LifeCategory.Explorer,
            60, 120, CostBand.Free, Daytime, true, true, 14, 0, ["street-photography", "neighborhoods"]),
        new("creativity-short-story", "300 kelimelik bir öykü yaz",
            "Bugün gördüğün bir sahneden yola çıkarak 300 kelimelik bir öykü yaz.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Creativity, LifeCategory.Learning,
            60, 120, CostBand.Free, Evening | Night, false, false, 14, 0, ["writing"]),
        new("creativity-craft", "Basit bir el işi projesi",
            "Basit bir el işi projesini (origami, örgü, seramik boyama…) baştan sona tamamla.",
            QuestType.Weekly, Difficulty.Medium, LifeCategory.Creativity, null,
            90, 180, CostBand.Low, Afternoon | Evening, false, false, 21, 0, ["crafts"]),
        new("creativity-song-cover", "Bir şarkıyı öğren ve kaydet",
            "Sevdiğin bir şarkıyı bir enstrümanla ya da sesinle öğren ve kendin için kaydet.",
            QuestType.Epic, Difficulty.Hard, LifeCategory.Creativity, LifeCategory.Learning,
            300, 900, CostBand.Free, DayPart.Any, false, false, 60, 0, ["music-making", "live-music"])
    ];
}
