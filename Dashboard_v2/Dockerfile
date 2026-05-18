# ── Stage 1: build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Node.js 20 (necesario para que PublishRunWebpack compile el frontend React)
RUN apt-get update \
    && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copiar archivos de configuración de solución para aprovechar la caché de capas
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY Dashboard_v2.slnx ./

# Copiar solo los .csproj primero para restaurar NuGet con caché
COPY src/Web/Web.csproj                           src/Web/
COPY src/Application/Application.csproj          src/Application/
COPY src/Domain/Domain.csproj                     src/Domain/
COPY src/Infrastructure/Infrastructure.csproj     src/Infrastructure/

RUN dotnet restore src/Web/Web.csproj

# Copiar el resto del código fuente
COPY src/ src/

# Publicar (PublishRunWebpack ejecuta automáticamente npm install + npm run build)
RUN dotnet publish src/Web/Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Copiar los archivos de datos estáticos (no incluidos en dotnet publish por defecto)
RUN cp -r src/Web/data /app/publish/data

# ── Stage 2: runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

# Puerto HTTP por defecto para contenedores ASP.NET Core
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Dashboard_v2.Web.dll"]
