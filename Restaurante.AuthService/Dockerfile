# Etapa 1: Base para ejecutar (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Etapa 2: Construcción (Build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos .csproj de cada capa respetando la estructura
COPY ["Restaurante.AuthService.Api/Restaurante.AuthService.Api.csproj", "Restaurante.AuthService.Api/"]
COPY ["Restaurante.AuthService.Application/Restaurante.AuthService.Application.csproj", "Restaurante.AuthService.Application/"]
COPY ["Restaurante.AuthService.Domain/Restaurante.AuthService.Domain.csproj", "Restaurante.AuthService.Domain/"]
COPY ["Restaurante.AuthService.Infrastructure/Restaurante.AuthService.Infrastructure.csproj", "Restaurante.AuthService.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "Restaurante.AuthService.Api/Restaurante.AuthService.Api.csproj"

# Copiar el resto del código de todas las capas
COPY . .

# Compilar el proyecto principal
WORKDIR "/src/Restaurante.AuthService.Api"
RUN dotnet build "Restaurante.AuthService.Api.csproj" -c Release -o /app/build

# Etapa 3: Publicación (Publish)
FROM build AS publish
RUN dotnet publish "Restaurante.AuthService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 4: Imagen Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Restaurante.AuthService.Api.dll"]