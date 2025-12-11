FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["02-Backend/Talos.Server/Talos.Server.csproj", "02-Backend/Talos.Server/"]
RUN dotnet restore "02-Backend/Talos.Server/Talos.Server.csproj"
COPY . .
WORKDIR "/src/02-Backend/Talos.Server"
RUN dotnet build "./Talos.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Talos.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Talos.Server.dll"]
