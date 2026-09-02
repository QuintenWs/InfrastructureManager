# Development — sla secrets lokaal op (buiten het project)
dotnet user-secrets init --project InfrastructureManager.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=InfrastructureManager_Dev;..." \
  --project InfrastructureManager.Web


For production:
  export ConnectionStrings__DefaultConnection="Server=...;Password=..."

  SQL SERVER user only:
  CREATE LOGIN InfraApp WITH PASSWORD = 'VeryStr0ng!RandomPw#2024';
CREATE USER InfraApp FOR LOGIN InfraApp;
ALTER ROLE db_datareader ADD MEMBER InfraApp;
ALTER ROLE db_datawriter ADD MEMBER InfraApp;
-- Geen db_owner rechten!


docker run -d --name sqlserver-dev -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Dev@Password123!" -p 1433:1433 \mcr.microsoft.com/mssql/server:2022-latest