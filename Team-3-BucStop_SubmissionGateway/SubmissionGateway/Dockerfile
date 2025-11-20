FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["SubmissionGateway.csproj", "./"]
RUN dotnet restore "SubmissionGateway.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "SubmissionGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SubmissionGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubmissionGateway.dll"]

