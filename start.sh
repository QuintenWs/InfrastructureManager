#!/bin/bash
cd "$(dirname "$0")"

export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=InfrastructureManager_Dev;User Id=sa;Password=Infra@2024!;TrustServerCertificate=True;'
export ASPNETCORE_ENVIRONMENT=Development

echo "Building..."
dotnet build InfrastructureManager.Web/InfrastructureManager.Web.csproj -c Debug --nologo -v q

if [ $? -eq 0 ]; then
    echo "Starting InfrastructureManager on http://localhost:5000"
    ./InfrastructureManager.Web/bin/Debug/net8.0/InfrastructureManager.Web
else
    echo "Build failed!"
fi
