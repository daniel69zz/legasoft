# Imagen genérica para los servicios cloud: docker build --build-arg PROJECT=Identity.Api
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/shared/ src/shared/
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/${PROJECT}/${PROJECT}.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG PROJECT
ENV APP_DLL=${PROJECT}.dll ASPNETCORE_URLS=http://+:8080
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]
