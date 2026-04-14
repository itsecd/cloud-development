FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CourseManagement.Storage/CourseManagement.Storage.csproj", "CourseManagement.Storage/"]
RUN dotnet restore "./CourseManagement.Storage/CourseManagement.Storage.csproj"
COPY . .
WORKDIR "/src/CourseManagement.Storage"
RUN dotnet build "./CourseManagement.Storage.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CourseManagement.Storage.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CourseManagement.Storage.dll"]