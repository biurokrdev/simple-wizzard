# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["D2ApiCreator.Api/D2ApiCreator.Api.csproj", "D2ApiCreator.Api/"]
COPY ["D2ApiCreator.Application/D2ApiCreator.Application.csproj", "D2ApiCreator.Application/"]
COPY ["D2ApiCreator.Domain/D2ApiCreator.Domain.csproj", "D2ApiCreator.Domain/"]
COPY ["D2ApiCreator.Infrastructure/D2ApiCreator.Infrastructure.csproj", "D2ApiCreator.Infrastructure/"]

# Restore dependencies (includes FluentValidation, EF Core, MediatR, DocumentFormat.OpenXml)
RUN dotnet restore "D2ApiCreator.Api/D2ApiCreator.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/D2ApiCreator.Api"
RUN dotnet build "D2ApiCreator.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "D2ApiCreator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory and temp directory for file uploads
RUN mkdir -p /app/logs /app/temp

# Set permissions
RUN chown -R app:app /app

# Switch to non-root user
USER app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_HTTP_PORTS=8080
ENV TZ=Europe/Warsaw

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "D2ApiCreator.Api.dll"]
