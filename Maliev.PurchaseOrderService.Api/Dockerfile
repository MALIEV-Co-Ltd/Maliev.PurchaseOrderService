# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config for private feed access
COPY nuget.config ./

# Copy project files
COPY Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj Maliev.PurchaseOrderService.Api/
COPY Maliev.PurchaseOrderService.Data/Maliev.PurchaseOrderService.Data.csproj Maliev.PurchaseOrderService.Data/
COPY Maliev.PurchaseOrderService.Common/Maliev.PurchaseOrderService.Common.csproj Maliev.PurchaseOrderService.Common/

# Restore with BuildKit secrets for private NuGet feed
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/Maliev.PurchaseOrderService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Use existing non-root 'app' user from base image
USER app

# Copy published files
COPY --from=build --chown=app:app /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/purchase-orders/liveness || exit 1

# Expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "Maliev.PurchaseOrderService.Api.dll"]
