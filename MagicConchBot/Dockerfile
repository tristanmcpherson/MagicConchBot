FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS base
WORKDIR /app

RUN apt-get clean && apt-get update
RUN apt-get install -y software-properties-common
RUN apt-get install -y \
	libopus0 \
	libopus-dev \
	libsodium23 \
	libsodium-dev \
	python3.10 \
	python3-pip \
	ffmpeg

# Install the latest version of yt-dlp using pip
RUN pip3 install --no-cache-dir --upgrade yt-dlp

# Verify yt-dlp installation
RUN yt-dlp --version

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

# Add a healthcheck to verify yt-dlp is working
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD yt-dlp --version || exit 1

ENTRYPOINT ["dotnet", "MagicConchBot.dll"]
