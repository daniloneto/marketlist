# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["src/MarketList.API/MarketList.API.csproj", "MarketList.API/"]
COPY ["src/MarketList.Application/MarketList.Application.csproj", "MarketList.Application/"]
COPY ["src/MarketList.Domain/MarketList.Domain.csproj", "MarketList.Domain/"]
COPY ["src/MarketList.Infrastructure/MarketList.Infrastructure.csproj", "MarketList.Infrastructure/"]

RUN dotnet restore "MarketList.API/MarketList.API.csproj"

# Copy source
COPY src/ .

WORKDIR "/src/MarketList.API"
RUN dotnet publish "MarketList.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

EXPOSE 8080

# Criar diretório para SQLite (se usar)
RUN mkdir -p /data

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MarketList.API.dll"]
