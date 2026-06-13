FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY src/ src/
RUN dotnet restore src/Aureus.Api/Aureus.Api.csproj
RUN dotnet publish src/Aureus.Api/Aureus.Api.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Aureus.Api.dll"]
