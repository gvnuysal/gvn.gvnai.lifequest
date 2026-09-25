# Derleme bağlamı repo köküdür: docker build -f src/LifeQuest.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce yalnızca proje ve paket tanımları: bağımlılık katmanı kod değişikliklerinde yeniden kullanılır.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY packages/ packages/
COPY src/LifeQuest.Domain/LifeQuest.Domain.csproj src/LifeQuest.Domain/
COPY src/LifeQuest.Application/LifeQuest.Application.csproj src/LifeQuest.Application/
COPY src/LifeQuest.Infrastructure/LifeQuest.Infrastructure.csproj src/LifeQuest.Infrastructure/
COPY src/LifeQuest.Api/LifeQuest.Api.csproj src/LifeQuest.Api/
RUN dotnet restore src/LifeQuest.Api/LifeQuest.Api.csproj

COPY src/LifeQuest.Domain/ src/LifeQuest.Domain/
COPY src/LifeQuest.Application/ src/LifeQuest.Application/
COPY src/LifeQuest.Infrastructure/ src/LifeQuest.Infrastructure/
COPY src/LifeQuest.Api/ src/LifeQuest.Api/
RUN dotnet publish src/LifeQuest.Api/LifeQuest.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false \
    && mkdir -p /app/logs

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build --chown=$APP_UID /app ./

# Root olmayan kullanıcı (imajın hazır "app" kullanıcısı).
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0
EXPOSE 8080

ENTRYPOINT ["dotnet", "LifeQuest.Api.dll"]
