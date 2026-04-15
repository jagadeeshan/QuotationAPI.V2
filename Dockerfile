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

# Render provides PORT dynamically. Bind Kestrel to all interfaces.
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "QuotationAPI.V2.dll"]
