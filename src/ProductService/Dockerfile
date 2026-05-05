FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductService.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Render uses PORT env variable
ENV ASPNETCORE_URLS=http://+:${PORT:-80}
EXPOSE 80
ENTRYPOINT ["dotnet", "ProductService.dll"]
