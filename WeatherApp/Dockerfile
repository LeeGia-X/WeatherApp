# Use .NET 9.0 preview SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0.300 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Use .NET 9.0 preview ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0.300 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Expose the default port for ASP.NET Core
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WeatherApp.dll"]
