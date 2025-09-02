# Maliev.PurchaseOrderService

This repository contains the Maliev Purchase Order Service. For details on the migration process and how to set up and run the service, please refer to [GEMINI.md](GEMINI.md).

## ACTION REQUIRED

To run the migrated service locally, you need to ensure your local user secrets are correctly configured. You can set them using the following commands:

```bash
dotnet user-secrets set "Jwt:Issuer" "your_issuer" --project Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj
dotnet user-secrets set "Jwt:Audience" "your_audience" --project Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj
dotnet user-secrets set "JwtSecurityKey" "thisisalongsecretkeyforjwttokenvalidationthatisatleast32characterslong" --project Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj
dotnet user-secrets set "ConnectionStrings:PurchaseOrderDbContext" "Server=(localdb)\mssqllocaldb;Database=PurchaseOrderServiceDb;Trusted_Connection=True;MultipleActiveResultSets=true" --project Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj
```

After setting the secrets, you can run the API project:

```bash
dotnet run --project Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj
```

The API will be accessible, and Swagger UI will be available at `/swagger` (e.g., `https://localhost:7000/swagger` or `http://localhost:5000/swagger`). Remember that the base path is `/purchaseorders`, so API endpoints will be like `/purchaseorders/v1/addresses`.
