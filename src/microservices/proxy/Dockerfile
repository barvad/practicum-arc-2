FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8000
ENV ASPNETCORE_URLS=http://+:8000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "CinemaAbyss.Proxy.csproj"

RUN dotnet build "CinemaAbyss.Proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CinemaAbyss.Proxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CinemaAbyss.Proxy.dll"]
