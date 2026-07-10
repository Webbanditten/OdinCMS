# Stage 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
EXPOSE 80
EXPOSE 443
COPY . .
RUN dotnet restore -v diag
RUN dotnet publish -c Release -o /app

# Stage 2
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --allow-unauthenticated curl
RUN curl -sL https://deb.nodesource.com/setup_22.x | bash -
RUN apt-get install -y --allow-unauthenticated nodejs
RUN rm -rf /var/lib/apt/lists/*
ENV TZ="Europe/Copenhagen"
# .NET 8+ aspnet images default to port 8080; keep listening on 80 as before
ENV ASPNETCORE_HTTP_PORTS=80
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "KeplerCMS.dll"]
