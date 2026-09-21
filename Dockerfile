FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["src/UrlShortener.Domain/UrlShortener.Domain.csproj", "src/UrlShortener.Domain/"]
COPY ["src/UrlShortener.Application/UrlShortener.Application.csproj", "src/UrlShortener.Application/"]
COPY ["src/UrlShortener.Infrastructure/UrlShortener.Infrastructure.csproj", "src/UrlShortener.Infrastructure/"]
COPY ["src/UrlShortener.WebApi/UrlShortener.WebApi.csproj", "src/UrlShortener.WebApi/"]

RUN dotnet restore "src/UrlShortener.WebApi/UrlShortener.WebApi.csproj"

COPY . .

WORKDIR "/src/src/UrlShortener.WebApi"
RUN dotnet publish "UrlShortener.WebApi.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "UrlShortener.WebApi.dll"]