FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Install clang/zlib1g-dev dependencies for publishing to native
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FakeTrafficMaker.App/FakeTrafficMaker.App.csproj", "FakeTrafficMaker.App/"]
RUN dotnet restore "./FakeTrafficMaker.App/FakeTrafficMaker.App.csproj"
COPY . .
WORKDIR "/src/FakeTrafficMaker.App"
RUN dotnet build "./FakeTrafficMaker.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FakeTrafficMaker.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./FakeTrafficMaker.App"]