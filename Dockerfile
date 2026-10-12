# CODE ATTRIBUTION (IIE Harvard Anglia)
# The separate SDK-build and Functions-runtime stages follow Docker's multi-stage-build pattern (Docker, n.d.-d); the .NET isolated worker model is documented by Microsoft (Microsoft, n.d.-c).
# The specific base images and publish paths are configured for this CoffeeNChill project. See docs/CODE_ATTRIBUTION.md.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY CoffeeNChillFunctions/CoffeeNChillFunctions.csproj CoffeeNChillFunctions/

RUN dotnet restore CoffeeNChillFunctions/CoffeeNChillFunctions.csproj

COPY CoffeeNChillFunctions/ CoffeeNChillFunctions/

RUN dotnet publish CoffeeNChillFunctions/CoffeeNChillFunctions.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build /app/publish /home/site/wwwroot