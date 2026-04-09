FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY src/TestServer/TestServer.csproj ./TestServer/
RUN dotnet restore TestServer/TestServer.csproj

COPY src/TestServer/ ./TestServer/
RUN dotnet publish TestServer/TestServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
RUN apk add --no-cache openssl icu-libs icu-data-full

WORKDIR /app
COPY --from=build /app .
COPY config/ /app/config/

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_EnableDiagnostics=0

EXPOSE 4840

ENTRYPOINT ["dotnet", "TestServer.dll"]
