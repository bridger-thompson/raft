FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["RaftNode/RaftNode.csproj", "RaftNode/"]
RUN dotnet restore "RaftNode/RaftNode.csproj"

COPY . .
WORKDIR /source/RaftNode
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "RaftNode.dll"]