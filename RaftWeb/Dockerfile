# Step 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore as distinct layers
COPY ["RaftWeb/RaftWeb.csproj", "RaftWeb/"]
RUN dotnet restore "RaftWeb/RaftWeb.csproj"

# Copy everything else and build
COPY . .
WORKDIR /source/RaftWeb
RUN dotnet publish -c Release -o /app

# Step 2: Serve the app using nginx
FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# Remove default nginx static assets
RUN rm -rf ./*

# Copy static assets from builder stage
COPY --from=build /app/wwwroot .

# Copy nginx configuration (if you have one)
COPY nginx.conf /etc/nginx/nginx.conf

# No need for an ENTRYPOINT here as we're using nginx's default command
