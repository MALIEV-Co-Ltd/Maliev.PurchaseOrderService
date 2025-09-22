# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution file
COPY ["Maliev.PurchaseOrderService.sln", "."]

# Copy project files for restore (improved layer caching)
COPY ["Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj", "Maliev.PurchaseOrderService.Api/"]
COPY ["Maliev.PurchaseOrderService.Data/Maliev.PurchaseOrderService.Data.csproj", "Maliev.PurchaseOrderService.Data/"]
COPY ["Maliev.PurchaseOrderService.Common/Maliev.PurchaseOrderService.Common.csproj", "Maliev.PurchaseOrderService.Common/"]
COPY ["Maliev.PurchaseOrderService.Tests/Maliev.PurchaseOrderService.Tests.csproj", "Maliev.PurchaseOrderService.Tests/"]

# Restore dependencies
RUN dotnet restore "Maliev.PurchaseOrderService.sln"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/Maliev.PurchaseOrderService.Api"
RUN dotnet build "Maliev.PurchaseOrderService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Maliev.PurchaseOrderService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

# Copy published application
COPY --from=publish /app/publish .

# Change ownership of the app directory to the non-root user
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/purchaseorders/liveness || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Maliev.PurchaseOrderService.Api.dll"]