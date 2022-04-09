FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

RUN apt-get clean && apt-get update && apt-get install -y \
	libopus0 \
	libopus-dev \
	libsodium23 \
	libsodium-dev \
	youtube-dl \
	ffmpeg \
	ffprobe \
	python

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
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
