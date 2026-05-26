# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first — maximises layer cache on source changes
COPY BankingService.sln .
COPY src/BankingService.Domain/BankingService.Domain.csproj                   src/BankingService.Domain/
COPY src/BankingService.Application/BankingService.Application.csproj         src/BankingService.Application/
COPY src/BankingService.Infrastructure/BankingService.Infrastructure.csproj   src/BankingService.Infrastructure/
COPY src/BankingService.Api/BankingService.Api.csproj                         src/BankingService.Api/

RUN dotnet restore

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/BankingService.Api/BankingService.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --no-create-home appuser
USER appuser

COPY --from=build /app/publish .

# SQLite database lives here — mount a volume for persistence
# docker run -v banking_data:/app/data banking-service
VOLUME ["/app/data"]

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/banking.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "BankingService.Api.dll"]