# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer-cached as long as .csproj doesn't change)
COPY JMAPI.csproj .
RUN dotnet restore JMAPI.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish JMAPI.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Kestrel listens on port 5000 inside the container (HTTP only — nginx handles TLS)
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000
ENTRYPOINT ["dotnet", "JMAPI.dll"]
