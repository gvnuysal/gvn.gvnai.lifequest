using System.Net;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Tests;

public sealed class ApiErrorTests
{
    [Fact]
    public void Framework_errors_are_kept_as_they_are()
    {
        var ex = ApiErrorParser.Parse(HttpStatusCode.Conflict,
            """[{"code":"QUEST_NOT_OFFERED","message":"Bu görev artık kabul edilemez.","type":"Conflict"}]""");
        Assert.True(ex.Has("QUEST_NOT_OFFERED"));
        Assert.Equal(ErrorType.Conflict, ex.Errors[0].Type);
        Assert.Equal("Bu görev artık kabul edilemez.", ex.Message);
    }

    [Fact]
    public void Validation_problem_details_map_to_camel_case_fields()
    {
        var ex = ApiErrorParser.Parse(HttpStatusCode.BadRequest,
            """{"title":"x","errors":{"DisplayName":["Ad çok kısa."],"Password":["Şifre zayıf.","İkinci"]}}""");
        var fields = ex.FieldErrors();
        Assert.Equal("Ad çok kısa.", fields["displayName"]);
        Assert.Equal("Şifre zayıf.", fields["password"]);
        Assert.Equal(3, ex.Errors.Count);
    }

    [Fact]
    public void Error_codes_are_not_mistaken_for_fields()
    {
        var ex = ApiErrorParser.Parse(HttpStatusCode.BadRequest, """[{"code":"EMAIL_TAKEN","message":"x","type":"Validation"}]""");
        Assert.Empty(ex.FieldErrors());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "", "RATE_LIMITED")]
    [InlineData(HttpStatusCode.InternalServerError, """{"detail":"stack"}""", "SERVER")]
    [InlineData(HttpStatusCode.NotFound, "<html>", "HTTP_404")]
    [InlineData(HttpStatusCode.BadRequest, """{"detail":"Geçersiz istek"}""", "HTTP_400")]
    public void Other_bodies_become_a_single_readable_error(HttpStatusCode status, string body, string code)
    {
        using var _ = new LanguageScope(AppLanguage.En);
        var ex = ApiErrorParser.Parse(status, body);
        Assert.Equal(code, Assert.Single(ex.Errors).Code);
        Assert.DoesNotContain("stack", ex.Message);
    }

    [Fact]
    public void Fallback_messages_follow_the_language()
    {
        using (new LanguageScope(AppLanguage.Tr))
            Assert.Equal("Sunucuya ulaşılamıyor. Bağlantını kontrol et.", ApiException.Network().Message);
        using (new LanguageScope(AppLanguage.En))
            Assert.Equal("Can't reach the server. Check your connection.", ApiException.Network().Message);
    }
}
