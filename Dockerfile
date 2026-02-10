FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 5000
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files - paths from root context
COPY ["src/MarketList.API/MarketList.API.csproj", "MarketList.API/"]
COPY ["src/MarketList.Application/MarketList.Application.csproj", "MarketList.Application/"]
COPY ["src/MarketList.Domain/MarketList.Domain.csproj", "MarketList.Domain/"]
COPY ["src/MarketList.Infrastructure/MarketList.Infrastructure.csproj", "MarketList.Infrastructure/"]

RUN dotnet restore "MarketList.API/MarketList.API.csproj"

# Copy all source files
COPY src/ .

WORKDIR "/src/MarketList.API"
RUN dotnet build "MarketList.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketList.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src /app/src

# Defaults para SQLite (podem ser sobrescritos via docker-compose ou Cloud Run)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV Database__Provider=Sqlite
ENV Database__ConnectionStrings__Sqlite="Data Source=/data/marketlist.db"
ENV Api__AllowedOrigins__0=https://marketlist-one.vercel.app

# Criar diretório para o banco SQLite
RUN mkdir -p /data

WORKDIR /app
ENTRYPOINT ["dotnet", "MarketList.API.dll"]
