# syntax=docker/dockerfile:1

# Stage 1: build the React frontend
FROM node:24-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2: build and publish the ASP.NET Core API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/OrderManagement.Api/OrderManagement.Api.csproj src/OrderManagement.Api/
RUN dotnet restore src/OrderManagement.Api/OrderManagement.Api.csproj
COPY src/OrderManagement.Api/ src/OrderManagement.Api/
RUN dotnet publish src/OrderManagement.Api/OrderManagement.Api.csproj \
    --configuration Release --no-restore --output /app

# Stage 3: runtime image with API and frontend, running as non-root
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/frontend/dist ./wwwroot

# The data directory must be owned by the non-root user. Docker copies this ownership
# into a named volume the first time the empty volume is mounted at /data.
RUN mkdir -p /data && chown "$APP_UID" /data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__DefaultConnection="Data Source=/data/app.db"

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderManagement.Api.dll"]
