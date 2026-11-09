# ---------- Stage 1: build the app ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first and restore the packages.
# Docker remembers this step, so rebuilds are fast unless the packages change.
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# Copy the rest of the code and publish a release build.
COPY . ./
RUN dotnet publish -c Release -o /app

# ---------- Stage 2: run the app ----------
# The runtime image is much smaller than the SDK image, because it has no build tools.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# The app listens on port 8080 inside the container.
ENV ASPNETCORE_URLS=http://+:8080
# Inside a container, "localhost" means the container itself, so point at the API on the host computer.
ENV ApiBaseUrl=http://host.docker.internal:5100
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]


# Run as a normal user, not as root (the .NET images include a user called "app").
USER app
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]