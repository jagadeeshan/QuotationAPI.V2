# Multi-stage Docker build for ASP.NET Core 8 Web API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore first to improve build cache usage
COPY QuotationAPI.V2.csproj ./
RUN dotnet restore "QuotationAPI.V2.csproj"

# Copy the remaining source and publish
COPY . .
RUN dotnet publish "QuotationAPI.V2.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render provides PORT at runtime (default 10000).
# Use shell-form CMD so $PORT is expanded at container start, NOT at build time.
# ENTRYPOINT exec form cannot expand env vars; CMD shell form can.
CMD dotnet QuotationAPI.V2.dll --urls "http://0.0.0.0:${PORT:-10000}"
