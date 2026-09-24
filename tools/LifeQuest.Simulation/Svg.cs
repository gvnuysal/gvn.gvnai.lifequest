using System.Globalization;
using System.Security;
using System.Text;

namespace LifeQuest.Simulation;

/// <summary>
/// Statik SVG grafikler (README/rapor içinde &lt;img&gt; olarak). Kendi açık zeminini taşır; kategorik renkler
/// doğrulanmış paletin ilk slotlarıdır; metinler seri rengi değil mürekkep rengidir; çizgi uçlarında doğrudan
/// etiket + lejant vardır; aynı sayılar raporda tablo olarak da bulunur (erişilebilirlik).
/// </summary>
internal static class Svg
{
    public static readonly string[] Series = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100"];
    private const string Surface = "#fcfcfb";
    private const string Ink = "#0b0b0b";
    private const string Ink2 = "#52514e";
    private const string Grid = "#e9e8e4";
    private const string Font = "Nunito, 'Segoe UI', Helvetica, Arial, sans-serif";

    private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
    private static string E(string s) => SecurityElement.Escape(s);

    public static string LineChart(string title, string subtitle, IReadOnlyList<(string Name, double[] Values)> series, string xLabel)
    {
        const int w = 800, h = 380, left = 58, right = 190, top = 96, bottom = 50;
        var plotW = w - left - right;
        var plotH = h - top - bottom;
        var n = series[0].Values.Length;
        double X(int i) => left + plotW * i / (double)(n - 1);
        double Y(double v) => top + plotH * (1 - v);

        var sb = Open(w, h, title, subtitle);

        // Lejant (tek satır)
        var lx = left;
        for (var s = 0; s < series.Count; s++)
        {
            sb.Append($"<rect x=\"{lx}\" y=\"70\" width=\"14\" height=\"4\" rx=\"2\" fill=\"{Series[s]}\"/>");
            sb.Append($"<text x=\"{lx + 20}\" y=\"76\" font-size=\"13\" fill=\"{Ink2}\">{E(series[s].Name)}</text>");
            lx += 30 + (int)(series[s].Name.Length * 7.2);
        }

        foreach (var tick in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            sb.Append($"<line x1=\"{left}\" x2=\"{left + plotW}\" y1=\"{F(Y(tick))}\" y2=\"{F(Y(tick))}\" stroke=\"{Grid}\" stroke-width=\"1\"/>");
            sb.Append($"<text x=\"{left - 8}\" y=\"{F(Y(tick) + 4)}\" font-size=\"12\" fill=\"{Ink2}\" text-anchor=\"end\">%{tick * 100:0}</text>");
        }
        foreach (var day in new[] { 1, 5, 10, 15, 20, 25, 30 }.Where(d => d <= n))
            sb.Append($"<text x=\"{F(X(day - 1))}\" y=\"{h - bottom + 20}\" font-size=\"12\" fill=\"{Ink2}\" text-anchor=\"middle\">{day}</text>");
        sb.Append($"<text x=\"{left + plotW / 2}\" y=\"{h - 10}\" font-size=\"12\" fill=\"{Ink2}\" text-anchor=\"middle\">{E(xLabel)}</text>");

        // Çizgiler + uç etiketleri (çakışmayı önlemek için dikey olarak ayrılır)
        var ends = series.Select((s, i) => (Index: i, Y: Y(s.Values[^1]))).OrderBy(e => e.Y).ToList();
        for (var i = 1; i < ends.Count; i++)
            if (ends[i].Y - ends[i - 1].Y < 18) ends[i] = ends[i] with { Y = ends[i - 1].Y + 18 };

        for (var s = 0; s < series.Count; s++)
        {
            var points = string.Join(" ", series[s].Values.Select((v, i) => $"{F(X(i))},{F(Y(v))}"));
            sb.Append($"<polyline points=\"{points}\" fill=\"none\" stroke=\"{Series[s]}\" stroke-width=\"2\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");
            for (var i = 0; i < n; i++)
                sb.Append($"<circle cx=\"{F(X(i))}\" cy=\"{F(Y(series[s].Values[i]))}\" r=\"6\" fill=\"transparent\"><title>{E(series[s].Name)} · gün {i + 1}: %{series[s].Values[i] * 100:0}</title></circle>");

            var end = ends.Single(e => e.Index == s);
            sb.Append($"<circle cx=\"{F(X(n - 1))}\" cy=\"{F(Y(series[s].Values[^1]))}\" r=\"4\" fill=\"{Series[s]}\" stroke=\"{Surface}\" stroke-width=\"2\"/>");
            sb.Append($"<text x=\"{left + plotW + 12}\" y=\"{F(end.Y + 4)}\" font-size=\"13\" fill=\"{Ink}\"><tspan font-weight=\"800\">%{series[s].Values[^1] * 100:0}</tspan> {E(series[s].Name)}</text>");
        }

        return Close(sb);
    }

    public static string BarPanels(string title, string subtitle, IReadOnlyList<(string Title, Func<double, string> Format, double Max, IReadOnlyList<(string Label, double Value, bool Emphasis)> Bars)> panels)
    {
        const int w = 860, top = 84, labelW = 170, gap = 28, barH = 16, rowH = 30;
        var rows = panels.Max(p => p.Bars.Count);
        var h = top + 34 + rows * rowH + 24;
        var panelW = (w - 20 - labelW - 20 - gap * (panels.Count - 1)) / panels.Count;
        var sb = Open(w, h, title, subtitle);

        // Satır etiketleri tek sütunda; paneller aynı satırları paylaşır (small multiples, ortak ölçek yok).
        for (var i = 0; i < panels[0].Bars.Count; i++)
        {
            var bar = panels[0].Bars[i];
            sb.Append($"<text x=\"20\" y=\"{top + 34 + i * rowH + 12}\" font-size=\"12.5\" fill=\"{Ink}\"{(bar.Emphasis ? " font-weight=\"800\"" : "")}>{E(bar.Label)}</text>");
        }

        for (var p = 0; p < panels.Count; p++)
        {
            var panel = panels[p];
            var barX = 20 + labelW + p * (panelW + gap);
            var barMaxW = panelW - 50;
            sb.Append($"<text x=\"{barX}\" y=\"{top + 14}\" font-size=\"13\" font-weight=\"800\" fill=\"{Ink}\">{E(panel.Title)}</text>");

            for (var i = 0; i < panel.Bars.Count; i++)
            {
                var bar = panel.Bars[i];
                var y = top + 34 + i * rowH;
                var bw = Math.Max(3, barMaxW * Math.Clamp(bar.Value / panel.Max, 0, 1));
                sb.Append($"<rect x=\"{barX}\" y=\"{y}\" width=\"{F(barMaxW)}\" height=\"{barH}\" rx=\"4\" fill=\"{Grid}\" opacity=\"0.6\"/>");
                sb.Append($"<rect x=\"{barX}\" y=\"{y}\" width=\"{F(bw)}\" height=\"{barH}\" rx=\"4\" fill=\"{Series[0]}\" opacity=\"{(bar.Emphasis ? "1" : "0.72")}\"><title>{E(bar.Label)} · {E(panel.Title)}: {E(panel.Format(bar.Value))}</title></rect>");
                sb.Append($"<text x=\"{F(barX + barMaxW + 8)}\" y=\"{y + 12}\" font-size=\"12.5\" fill=\"{Ink}\"{(bar.Emphasis ? " font-weight=\"800\"" : "")}>{E(panel.Format(bar.Value))}</text>");
            }
        }

        return Close(sb);
    }

    private static StringBuilder Open(int w, int h, string title, string subtitle)
    {
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {w} {h}\" width=\"{w}\" height=\"{h}\" font-family=\"{Font}\" role=\"img\" aria-label=\"{E(title)}\">");
        sb.Append($"<title>{E(title)}</title>");
        sb.Append($"<rect width=\"{w}\" height=\"{h}\" rx=\"14\" fill=\"{Surface}\" stroke=\"{Grid}\"/>");
        sb.Append($"<text x=\"20\" y=\"34\" font-size=\"18\" font-weight=\"800\" fill=\"{Ink}\">{E(title)}</text>");
        sb.Append($"<text x=\"20\" y=\"55\" font-size=\"13\" fill=\"{Ink2}\">{E(subtitle)}</text>");
        return sb;
    }

    private static string Close(StringBuilder sb) => sb.Append("</svg>").ToString();
}
