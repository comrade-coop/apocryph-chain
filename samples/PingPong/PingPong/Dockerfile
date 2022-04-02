FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["samples/PingPong/PingPong/PingPong.csproj", "PingPong/"]
RUN dotnet restore "samples/PingPong/PingPong/PingPong.csproj"
COPY . .
WORKDIR "/src/PingPong"
RUN dotnet build "PingPong.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PingPong.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PingPong.dll"]
