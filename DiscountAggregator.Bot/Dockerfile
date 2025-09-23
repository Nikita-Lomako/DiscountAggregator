# Dockerfile для отладки в Visual Studio
FROM mcr.microsoft.com/playwright/dotnet:v1.54.0-jammy AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Устанавливаем переменные окружения для Playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiscountAggregator.Bot/DiscountAggregator.Bot.csproj", "DiscountAggregator.Bot/"]
COPY ["DiscountAggregator.Application/DiscountAggregator.Application.csproj", "DiscountAggregator.Application/"]
COPY ["DiscountAggregator.Domain/DiscountAggregator.Domain.csproj", "DiscountAggregator.Domain/"]
COPY ["DiscountAggregator.Infrastructure/DiscountAggregator.Infrastructure.csproj", "DiscountAggregator.Infrastructure/"]
RUN dotnet restore "DiscountAggregator.Bot/DiscountAggregator.Bot.csproj"
COPY . .
WORKDIR "/src/DiscountAggregator.Bot"
RUN dotnet build "DiscountAggregator.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiscountAggregator.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscountAggregator.Bot.dll"]