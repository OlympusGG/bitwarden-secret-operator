FROM alpine:latest as downloader

ARG BW_VERSION=2022.11.0

RUN apk add wget unzip

RUN cd /tmp && wget https://github.com/bitwarden/clients/releases/download/cli-v${BW_VERSION}/bw-linux-${BW_VERSION}.zip && \
    unzip /tmp/bw-linux-${BW_VERSION}.zip

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Bitwarden.SecretOperator/Bitwarden.SecretOperator.csproj", "Bitwarden.SecretOperator/"]
RUN dotnet restore "Bitwarden.SecretOperator/Bitwarden.SecretOperator.csproj"
COPY . .
WORKDIR "/src/Bitwarden.SecretOperator"
RUN dotnet build "Bitwarden.SecretOperator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bitwarden.SecretOperator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=downloader /tmp/bw /usr/local/bin/bw

RUN chmod +x /usr/local/bin/bw

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bitwarden.SecretOperator.dll"]
