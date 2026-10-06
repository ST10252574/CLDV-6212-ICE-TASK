# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (separate layer so NuGet packages are cached between builds)
COPY StockSense.Api/StockSense.Api.csproj StockSense.Api/
RUN dotnet restore StockSense.Api/StockSense.Api.csproj

COPY StockSense.Api/ StockSense.Api/
RUN dotnet publish StockSense.Api/StockSense.Api.csproj -c Release -o /app/publish --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as the non-root user that ships in the official image
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StockSense.Api.dll"]
