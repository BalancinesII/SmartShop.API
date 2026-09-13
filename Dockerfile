# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first and restore — leverages Docker layer caching so
# dependencies are only re-fetched when a .csproj actually changes.
COPY SmartShop.API/SmartShop.API.csproj SmartShop.API/
COPY SmartShop.Application/SmartShop.Application.csproj SmartShop.Application/
COPY SmartShop.Domain/SmartShop.Domain.csproj SmartShop.Domain/
COPY SmartShop.Infrastructure/SmartShop.Infrastructure.csproj SmartShop.Infrastructure/
RUN dotnet restore SmartShop.API/SmartShop.API.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish SmartShop.API/SmartShop.API.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The app reads config from environment variables (see docker-compose.yml).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SmartShop.API.dll"]
