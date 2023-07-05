FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["EC2RCHandler.csproj", "./"]
RUN dotnet restore "EC2RCHandler.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "EC2RCHandler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EC2RCHandler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EC2RCHandler.dll"]
