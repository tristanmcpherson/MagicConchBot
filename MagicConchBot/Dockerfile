FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

RUN apt-get clean && apt-get update && apt-get install -y \
	libopus-dev \
	libsodium-dev \
	ffmpeg \
	python

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY MagicConchBot/NuGet.config ./
COPY MagicConchBot/MagicConchBot.csproj MagicConchBot/
RUN dotnet restore MagicConchBot/MagicConchBot.csproj
COPY . .
WORKDIR /src/MagicConchBot
RUN dotnet build MagicConchBot.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish MagicConchBot.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MagicConchBot.dll"]
