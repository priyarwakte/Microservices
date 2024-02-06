FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5140

ENV ASPNETCORE_URLS=http://+:5140

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Play.Catalog.Contracts/Play.Catalog.Contracts.csproj", "src/Play.Catalog.Contracts/"]
COPY ["src/Play.Catalog.Service/Play.Catalog.Service.csproj", "src/Play.Catalog.Service/"]

RUN --mount=type=secret,id=GH_OWNER,dst=/GH_OWNER --mount=type=secret,id=GH_PAT,dst=/GH_PAT \
    dotnet nuget add source --username USERNAME --password `cat /GH_PAT` --store-password-in-clear-text --name github "https://nuget.pkg.github.com/`cat /GH_OWNER`/index.json"

RUN dotnet restore "src/Play.Catalog.Service/Play.Catalog.Service.csproj"
COPY ./src ./src

RUN dotnet publish "src/Play.Catalog.Service/Play.Catalog.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Play.Catalog.Service.dll"]
