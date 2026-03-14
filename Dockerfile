FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY GeneratorService/GeneratorService.csproj GeneratorService/
RUN dotnet restore GeneratorService/GeneratorService.csproj

COPY GeneratorService/ GeneratorService/
RUN dotnet publish GeneratorService/GeneratorService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GeneratorService.dll"]
