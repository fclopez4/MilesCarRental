# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos del proyecto y restaurar dependencias
COPY CarRentalSearch.Api/CarRentalSearch.Api.csproj CarRentalSearch.Api/
COPY CarRentalSearch.Domain/CarRentalSearch.Domain.csproj CarRentalSearch.Domain/
COPY CarRentalSearch.Infrastructure/CarRentalSearch.Infrastructure.csproj CarRentalSearch.Infrastructure/
COPY CarRentalSearch.sln ./
RUN dotnet restore

# Copiar todo el código fuente y compilar
COPY . .
RUN dotnet build CarRentalSearch.Api/CarRentalSearch.Api.csproj -c Release -o /app/build

# Etapa 2: Publish
FROM build AS publish
RUN dotnet publish CarRentalSearch.Api/CarRentalSearch.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# Exponer el puerto 80
EXPOSE 80

# Punto de entrada
ENTRYPOINT ["dotnet", "CarRentalSearch.Api.dll"]