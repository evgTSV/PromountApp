FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PromountApp.Bot.fsproj", "./"]
RUN dotnet restore "PromountApp.Bot.fsproj"
WORKDIR "/src/PromountApp.Bot"
COPY . .
RUN dotnet build "PromountApp.Bot.fsproj" -c Release -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PromountApp.Bot.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["dotnet", "PromountApp.Bot.dll"]