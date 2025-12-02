FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["GameInfoService.csproj", "./"]
RUN dotnet restore "GameInfoService.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "GameInfoService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameInfoService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameInfoService.dll"]

