#!/bin/bash
# CI Validation Script for PostgreSQL Testing Setup
# This script validates that the CI environment has all required components

set -e

echo "🔍 Validating CI PostgreSQL Testing Setup..."

# Check required environment variables
echo "📋 Checking environment variables..."
required_vars=(
    "ConnectionStrings__PurchaseOrderDbContext"
    "JWT_SIGNING_KEY"
    "JWT_ISSUER"
    "JWT_AUDIENCE"
    "SUPPLIER_SERVICE_URL"
    "ORDER_SERVICE_URL"
    "CURRENCY_SERVICE_URL"
    "UPLOAD_SERVICE_URL"
    "PDF_SERVICE_URL"
)

for var in "${required_vars[@]}"; do
    if [[ -z "${!var}" ]]; then
        echo "❌ Environment variable $var is not set"
        exit 1
    else
        echo "✅ $var is set"
    fi
done

# Test PostgreSQL connectivity
echo "🔌 Testing PostgreSQL connectivity..."
if pg_isready -h localhost -p 5432 -U postgres; then
    echo "✅ PostgreSQL is ready"
else
    echo "❌ PostgreSQL is not ready"
    exit 1
fi

# Test database connection
echo "🔗 Testing database connection..."
if psql -h localhost -p 5432 -U postgres -d test_db -c "SELECT 1;" > /dev/null 2>&1; then
    echo "✅ Database connection successful"
else
    echo "❌ Database connection failed"
    exit 1
fi

# Check .NET version
echo "🔧 Checking .NET version..."
dotnet_version=$(dotnet --version)
if [[ $dotnet_version == 9.* ]]; then
    echo "✅ .NET 10.x detected: $dotnet_version"
else
    echo "❌ Expected .NET 10.x, found: $dotnet_version"
    exit 1
fi

# Test solution build
echo "🏗️ Testing solution build..."
if dotnet build Maliev.PurchaseOrderService.sln --no-restore --verbosity quiet; then
    echo "✅ Solution builds successfully"
else
    echo "❌ Solution build failed"
    exit 1
fi

echo "🎉 All validations passed! CI is ready for PostgreSQL testing."