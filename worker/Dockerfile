FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["api/api.csproj", "api/"]
COPY ["worker/worker.csproj", "worker/"]
RUN dotnet restore "worker/worker.csproj"

COPY ["api/", "api/"]
COPY ["worker/", "worker/"]
RUN dotnet publish "worker/worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "worker.dll"]
