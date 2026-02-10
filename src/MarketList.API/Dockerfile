FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 5000
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files - adjusted paths for root context
COPY ["MarketList.API/MarketList.API.csproj", "MarketList.API/"]
COPY ["MarketList.Application/MarketList.Application.csproj", "MarketList.Application/"]
COPY ["MarketList.Domain/MarketList.Domain.csproj", "MarketList.Domain/"]
COPY ["MarketList.Infrastructure/MarketList.Infrastructure.csproj", "MarketList.Infrastructure/"]

RUN dotnet restore "MarketList.API/MarketList.API.csproj"

# Copy all source files
COPY . .

WORKDIR "/src/MarketList.API"
RUN dotnet build "MarketList.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketList.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src /app/src
WORKDIR /app
ENTRYPOINT ["dotnet", "MarketList.API.dll"]
