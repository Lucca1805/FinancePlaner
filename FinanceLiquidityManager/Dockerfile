#Layer 1
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
#Pull Microsoft ASP.NET 6 name it base
WORKDIR /app
#Navigate to /app this is the working dir for the container
EXPOSE 5200
#Expose it on Port 5200

ENV ASPNETCORE_URLS=http://*:5200
# Assigned a default url to the container


# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
# Layer 2
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# pull sdk for asp.net and nane it build
ARG configuration=Release
WORKDIR /src
# goto /src Folder
COPY ["FinanceLiquidityManager.csproj", "./"]
# Copy the Project to the source folder
RUN dotnet restore "FinanceLiquidityManager.csproj"
COPY . .
WORKDIR "/src/."
# Goto Copied Folder
RUN dotnet build "FinanceLiquidityManager.csproj" -c $configuration -o /app/build
# Run a dotnet Build set configuration as release and output directory to /app/build

# Layer 3 (publish layer)
FROM build AS publish
# Pull the build version from layer 2
ARG configuration=Release
RUN dotnet publish "FinanceLiquidityManager.csproj" -c $configuration -o /app/publish /p:UseAppHost=false
# RUn a dotnet publish set configuration as release and output directory to /app/build

#Layer 4 (final layer)
FROM base AS final
# pull the version name it final
WORKDIR /app
# goto /app
COPY --from=publish /app/publish .
# Copy from /publish to app/publish
ENTRYPOINT ["dotnet", "FinanceLiquidityManager.dll"]
# Set the Entrypoint
