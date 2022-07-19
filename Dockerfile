#FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
#WORKDIR /app

# Copy csproj and restore as distinct layers
#COPY *.csproj ./
#RUN dotnet restore

# Copy everything else and build
#COPY . ./
#RUN dotnet publish -c Release -o out

# Build runtime image
#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
#WORKDIR /app
#COPY --from=build-env /app/out .
#ENTRYPOINT ["dotnet", "KeplerCMS.dll"]


# Stage 1

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /build
EXPOSE 80
EXPOSE 443
COPY . .
RUN dotnet restore -v diag
RUN dotnet publish -c Release -o /app

# Stage 2
FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS final
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        #libc6-dev \
        libgdiplus 
        #libx11-dev \
RUN apt-get update && apt-get install -y --allow-unauthenticated curl
RUN curl -sL https://deb.nodesource.com/setup_16.x | bash -
RUN apt-get install -y --allow-unauthenticated nodejs
RUN  rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "KeplerCMS.dll"]
