# Use the latest official Microsoft .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the project file and restore dependencies
COPY ItsAWonderfulWorldAPI/*.csproj ./
RUN dotnet restore

# Copy the rest of the source code and build the application
COPY ItsAWonderfulWorldAPI/. ./
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "ItsAWonderfulWorldAPI.dll"]
