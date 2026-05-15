# syntax=docker/dockerfile:1
# بناء وتشغيل API على Railway (أو أي منصة Docker)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SCHOOLS-backend.sln ./
COPY src/Schools.Api/Schools.Api.csproj ./src/Schools.Api/
RUN dotnet restore ./src/Schools.Api/Schools.Api.csproj

COPY src/Schools.Api/ ./src/Schools.Api/
RUN dotnet publish ./src/Schools.Api/Schools.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway يضبط متغير PORT — الاستماع على كل الواجهات
CMD ["/bin/sh", "-c", "exec dotnet Schools.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
