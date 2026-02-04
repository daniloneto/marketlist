FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/MarketList.API/MarketList.API.csproj", "MarketList.API/"]
COPY ["src/MarketList.Application/MarketList.Application.csproj", "MarketList.Application/"]
COPY ["src/MarketList.Domain/MarketList.Domain.csproj", "MarketList.Domain/"]
COPY ["src/MarketList.Infrastructure/MarketList.Infrastructure.csproj", "MarketList.Infrastructure/"]
RUN dotnet restore "MarketList.API/MarketList.API.csproj"
COPY src/ .
WORKDIR "/src/MarketList.API"
RUN dotnet build "MarketList.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketList.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketList.API.dll"]
