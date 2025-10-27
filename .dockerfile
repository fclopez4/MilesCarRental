# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CarRentalSearch.Api/CarRentalSearch.Api.csproj CarRentalSearch.Api/
COPY CarRentalSearch.Application/CarRentalSearch.Application.csproj CarRentalSearch.Application/
COPY CarRentalSearch.Domain/CarRentalSearch.Domain.csproj CarRentalSearch.Domain/
COPY CarRentalSearch.Infrastructure/CarRentalSearch.Infrastructure.csproj CarRentalSearch.Infrastructure/
COPY CarRentalSearch.Test/CarRentalSearch.Test.csproj CarRentalSearch.Test/
COPY CarRentalSearch.sln ./
RUN dotnet restore
COPY . .
RUN dotnet build CarRentalSearch.Api/CarRentalSearch.Api.csproj -c Release -o /app/build

# Etapa 2: Publish
FROM build AS publish
RUN dotnet publish CarRentalSearch.Api/CarRentalSearch.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "CarRentalSearch.Api.dll"]