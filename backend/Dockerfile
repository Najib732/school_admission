# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SchoolAdmission.slnx ./
COPY SchoolAdmission/SchoolAdmission.csproj SchoolAdmission/
COPY SchoolAdmission.Tests/SchoolAdmission.Tests.csproj SchoolAdmission.Tests/
RUN dotnet restore SchoolAdmission.slnx
COPY . .
RUN dotnet publish SchoolAdmission/SchoolAdmission.csproj -c Release -o /app --no-restore

# Tests (docker compose --profile test run --rm backend-tests)
FROM build AS test
ENTRYPOINT ["dotnet", "test", "SchoolAdmission.slnx", "-c", "Release", "--logger", "console;verbosity=normal"]

# Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# The port and all other settings come from docker-compose.yml
ENV ASPNETCORE_HTTP_PORTS=
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SchoolAdmission.dll"]
