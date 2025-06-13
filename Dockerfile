# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source files and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Create runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/out ./

# Expose port your app listens on (adjust if needed)
EXPOSE 5000

# Start the application (replace YourProject.dll with your actual dll)
ENTRYPOINT ["dotnet", "tradex-backend.dll"]
