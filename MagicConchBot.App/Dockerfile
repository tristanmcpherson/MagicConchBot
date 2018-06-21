FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80:80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY MagicConchBot.App/MagicConchBot.App.csproj MagicConchBot.App/
RUN dotnet restore MagicConchBot.App/MagicConchBot.App.csproj
COPY . .
WORKDIR /src/MagicConchBot.App
RUN dotnet build MagicConchBot.App.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish MagicConchBot.App.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MagicConchBot.App.dll"]
