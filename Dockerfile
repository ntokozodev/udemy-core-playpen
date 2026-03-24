FROM node:20-bookworm-slim AS node

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY --from=node /usr/local /usr/local
RUN node --version && npm --version
RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*
COPY src src
RUN dotnet restore src/AuthPlaypen.Api/AuthPlaypen.Api.csproj
RUN dotnet publish src/AuthPlaypen.Api/AuthPlaypen.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthPlaypen.Api.dll"]
