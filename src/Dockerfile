# =============================================================================
# Corporate Memo System — Multi-stage Docker Build
# Build context: the src/ directory (where CorporateMemo.sln lives)
#
# Build:  docker build -t corporatememo:latest .
# Run:    docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="..." corporatememo:latest
# =============================================================================

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so NuGet restore is cached separately from source
COPY ["CorporateMemo.Web/CorporateMemo.Web.csproj",                 "CorporateMemo.Web/"]
COPY ["CorporateMemo.Application/CorporateMemo.Application.csproj", "CorporateMemo.Application/"]
COPY ["CorporateMemo.Domain/CorporateMemo.Domain.csproj",           "CorporateMemo.Domain/"]
COPY ["CorporateMemo.Infrastructure/CorporateMemo.Infrastructure.csproj", "CorporateMemo.Infrastructure/"]

RUN dotnet restore "CorporateMemo.Web/CorporateMemo.Web.csproj"

# Copy all remaining source files and publish
COPY . .
RUN dotnet publish "CorporateMemo.Web/CorporateMemo.Web.csproj" \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Port 8080 is the default for Azure App Service containers
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published output
COPY --from=build /app/publish .

# Create attachments directory (mount Azure Files here in production)
RUN mkdir -p /app/attachments

ENTRYPOINT ["dotnet", "CorporateMemo.Web.dll"]
