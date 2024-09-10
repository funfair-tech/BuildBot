FROM mcr.microsoft.com/dotnet/runtime-deps:8.0.8-jammy

WORKDIR /usr/src/app

# Bundle App Source
COPY BuildBot .
COPY appsettings.json .

RUN apt-get update && apt-get dist-upgrade -y && apt-get autoremove -y && apt-get clean

ENTRYPOINT [ "/usr/src/app/BuildBot" ]
 