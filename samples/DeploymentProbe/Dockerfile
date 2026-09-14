FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d AS build
WORKDIR /src
# Explicit copies keep unrelated repository files out of image layers.
COPY samples/DeploymentProbe/DeploymentProbe.csproj ./
RUN dotnet restore DeploymentProbe.csproj
COPY samples/DeploymentProbe/Program.cs ./
RUN dotnet publish DeploymentProbe.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:6a94333d37514e385650a3c81a55e5350b67253dbe136e9cf17e499c35606a8c AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DeploymentProbe.dll"]
