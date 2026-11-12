# Build context: the wizzard-template ROOT (not this folder) — the Services API references the
# Application/Infrastructure/Domain layers of D2ApiCreator.
#   podman build -f D2ServicesCreator/D2ServicesCreator.Api/Dockerfile -t d2creator-services-api .
# Root .dockerignore keeps D2GuiCreator, infra, bin/obj and logs out of the context.

# ── build ──────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first — restore is cached until a .csproj changes.
COPY D2ApiCreator/D2ApiCreator.Domain/D2ApiCreator.Domain.csproj D2ApiCreator/D2ApiCreator.Domain/
COPY D2ApiCreator/D2ApiCreator.Application/D2ApiCreator.Application.csproj D2ApiCreator/D2ApiCreator.Application/
COPY D2ApiCreator/D2ApiCreator.Infrastructure/D2ApiCreator.Infrastructure.csproj D2ApiCreator/D2ApiCreator.Infrastructure/
COPY D2ServicesCreator/D2ServicesCreator.Api/D2ServicesCreator.Api.csproj D2ServicesCreator/D2ServicesCreator.Api/
RUN dotnet restore D2ServicesCreator/D2ServicesCreator.Api/D2ServicesCreator.Api.csproj

COPY D2ApiCreator/D2ApiCreator.Domain/ D2ApiCreator/D2ApiCreator.Domain/
COPY D2ApiCreator/D2ApiCreator.Application/ D2ApiCreator/D2ApiCreator.Application/
COPY D2ApiCreator/D2ApiCreator.Infrastructure/ D2ApiCreator/D2ApiCreator.Infrastructure/
COPY D2ServicesCreator/D2ServicesCreator.Api/ D2ServicesCreator/D2ServicesCreator.Api/
RUN dotnet publish D2ServicesCreator/D2ServicesCreator.Api/D2ServicesCreator.Api.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── runtime ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl for the container healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R app:app /app
USER app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Docker \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Warsaw

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "D2ServicesCreator.Api.dll"]
