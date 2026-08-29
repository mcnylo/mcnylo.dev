FROM node:22-bookworm-slim AS assets
WORKDIR /src/mcnylo.dev

COPY mcnylo.dev/package*.json ./
RUN npm ci --include=optional

COPY mcnylo.dev/ ./
RUN npm run css:build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY mcnylo.dev.slnx ./
COPY mcnylo.dev/mcnylo.dev.csproj mcnylo.dev/

RUN dotnet restore mcnylo.dev/mcnylo.dev.csproj

COPY mcnylo.dev/ mcnylo.dev/
COPY --from=assets /src/mcnylo.dev/wwwroot/css/site.css mcnylo.dev/wwwroot/css/site.css

WORKDIR /src/mcnylo.dev

RUN dotnet publish mcnylo.dev.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

RUN mkdir -p /var/lib/mcnylo/media /var/lib/mcnylo/keys

EXPOSE 8080

ENTRYPOINT ["dotnet", "mcnylo.dev.dll"]
