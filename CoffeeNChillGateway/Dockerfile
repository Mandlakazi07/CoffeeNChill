# CODE ATTRIBUTION (IIE Harvard Anglia). Full reference list: docs/CODE_ATTRIBUTION.md
#
# (Docker, n.d.-d)
#   Title:    Multi-stage builds
#   Author:   Docker Inc.
#   Date:     n.d. (accessed 10 November 2026)
#   Version:  online documentation, no version number
#   Link:     https://docs.docker.com/build/building/multi-stage/
#   Used for: the separate SDK build stage and ASP.NET runtime stage.
#
# (Microsoft, n.d.-e)
#   Title:    Breaking change: Default ASP.NET Core port changed from 80 to 8080
#   Author:   Microsoft
#   Date:     n.d. (accessed 10 November 2026)
#   Version:  online documentation, no version number
#   Link:     https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port
#   Used for: listening on port 8080 (ASPNETCORE_HTTP_PORTS).
#
# (Microsoft, n.d.-f)
#   Title:    Breaking change: New non-root 'app' user in Linux images
#   Author:   Microsoft
#   Date:     n.d. (accessed 10 November 2026)
#   Version:  online documentation, no version number
#   Link:     https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/app-user
#   Used for: running the container as the non-root app user (USER $APP_UID).
#
# Base images: mcr.microsoft.com/dotnet/sdk:10.0 and mcr.microsoft.com/dotnet/aspnet:10.0 (Microsoft).
# The project paths and settings are CoffeeNChill configuration.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Restore first so the package layer is cached until the .csproj changes.
COPY CoffeeNChillGateway.csproj ./

RUN dotnet restore CoffeeNChillGateway.csproj

COPY . ./

RUN dotnet publish CoffeeNChillGateway.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

# The gateway only speaks HTTP inside the container. Compose maps it to the
# public port (5000 by default).
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "CoffeeNChillGateway.dll"]
