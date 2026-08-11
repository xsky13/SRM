# syntax=docker/dockerfile:1

# runtime stage small image for prod?
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# app runs from /app inside the container
WORKDIR /app

# the port in which the app listens inside the container
EXPOSE 8080

# make kestrel listen on port 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# curl for health checks
USER root
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*

# darle permisos a los logs de crear archivos
RUN mkdir -p /app/logs && chown -R app:app /app/logs

# switch back to normal user
USER app

# always checking if healthy
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
	CMD curl -fsS http://localhost:8080/health || exit 1

# BUILD STAGE
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# first only copy csproj? apparently makes for better docker caching?
COPY ["./SRM.Api/SRM.Api.csproj", "./SRM.Api/"]
RUN dotnet restore "./SRM.Api/SRM.Api.csproj"

# copy and build
COPY . .
RUN dotnet build "./SRM.Api/SRM.Api.csproj" -c Release -o /app/build

# PUBLISH STAGE
FROM build AS publish
RUN dotnet publish "./SRM.Api/SRM.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# FINAL STAGE
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SRM.Api.dll"]


#docker build . -t firstdocker:local
#docker run --rm -p 5000:8080 firstdocker:local
