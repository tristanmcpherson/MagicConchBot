FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS base
WORKDIR /app

RUN apt-get clean && apt-get update
RUN apt-get install -y software-properties-common
RUN apt-get install -y \
	libopus0 \
	libopus-dev \
	libsodium23 \
	libsodium-dev \
	youtube-dl \
	ffmpeg \
	python3.10

RUN DEBIAN_FRONTEND=noninteractive TZ="America/New_York" apt-get -y install tzdata

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

COPY MagicConchBot.csproj MagicConchBot.csproj
RUN dotnet restore MagicConchBot.csproj
COPY . .
WORKDIR /src/
RUN dotnet build MagicConchBot.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish MagicConchBot.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MagicConchBot.dll"]
