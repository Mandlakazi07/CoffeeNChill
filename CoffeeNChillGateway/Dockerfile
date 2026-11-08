# CODE ATTRIBUTION (IIE Harvard Anglia)
# The SDK-build and ASP.NET-runtime stages follow Docker's multi-stage-build pattern (Docker, n.d.-d).
# Running as the non-root app user and listening on 8080 follow the defaults of Microsoft's .NET container images.
# The project paths and settings are CoffeeNChill configuration. See docs/CODE_ATTRIBUTION.md.

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
