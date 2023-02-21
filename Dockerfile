FROM alpine:latest as downloader

ARG BW_VERSION=2023.2.0

RUN apk add wget unzip

RUN cd /tmp && wget https://github.com/bitwarden/clients/releases/download/cli-v${BW_VERSION}/bw-linux-${BW_VERSION}.zip && \
    unzip /tmp/bw-linux-${BW_VERSION}.zip

FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
WORKDIR /operator

COPY ./ ./
RUN dotnet publish -c Release -o out src/Bitwarden.SecretOperator/Bitwarden.SecretOperator.csproj

# The runner for the application
FROM mcr.microsoft.com/dotnet/aspnet:7.0 as final

RUN addgroup k8s-operator && useradd --create-home -G k8s-operator operator-user

WORKDIR /operator
COPY --from=build /operator/out/ ./
COPY --from=downloader /tmp/bw /usr/local/bin/bw

RUN chmod +x /usr/local/bin/bw
RUN chown operator-user:k8s-operator -R .

USER operator-user

ENTRYPOINT [ "dotnet", "Bitwarden.SecretOperator.dll" ]
