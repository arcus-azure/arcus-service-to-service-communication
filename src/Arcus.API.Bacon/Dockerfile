FROM mcr.microsoft.com/dotnet/aspnet:6.0.6-alpine3.14 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14 AS build
WORKDIR /src
COPY ["./Arcus.API.Bacon/Arcus.API.Bacon.csproj", ""]

COPY . .
WORKDIR "/src/."
RUN dotnet build "./Arcus.API.Bacon/Arcus.API.Bacon.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "./Arcus.API.Bacon/Arcus.API.Bacon.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Arcus.API.Bacon.dll"]
