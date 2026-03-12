# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files to restore
COPY ["Directory.Packages.props", "."]
COPY [".editorconfig", "."]
COPY ["src/RequiemNexus.slnx", "src/"]
COPY ["src/RequiemNexus.Web/RequiemNexus.Web.csproj", "src/RequiemNexus.Web/"]
COPY ["src/RequiemNexus.Application/RequiemNexus.Application.csproj", "src/RequiemNexus.Application/"]
COPY ["src/RequiemNexus.Domain/RequiemNexus.Domain.csproj", "src/RequiemNexus.Domain/"]
COPY ["src/RequiemNexus.Data/RequiemNexus.Data.csproj", "src/RequiemNexus.Data/"]

RUN dotnet restore "src/RequiemNexus.slnx"

# Copy the rest
COPY src/ src/
WORKDIR "/src/src/RequiemNexus.Web"

RUN dotnet build "RequiemNexus.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RequiemNexus.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create logging and data directories if needed
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /app/logs /app/.data && chown -R 1000:1000 /app

USER 1000
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RequiemNexus.Web.dll"]
