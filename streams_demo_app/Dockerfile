FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8010

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["streams_demo_app/streams_demo_app.csproj", "streams_demo_app/"]
RUN dotnet restore "streams_demo_app/streams_demo_app.csproj"
COPY . .
WORKDIR "/src/streams_demo_app"
RUN dotnet build "streams_demo_app.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "streams_demo_app.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "streams_demo_app.dll"]
