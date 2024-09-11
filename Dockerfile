FROM mcr.microsoft.com/dotnet/runtime-deps:8.0.8-jammy

WORKDIR /usr/src/app

# Bundle App Source
COPY BuildBot .
COPY appsettings.json .
COPY healthcheck .

RUN apt-get update && apt-get dist-upgrade -y && apt-get autoremove -y && apt-get clean

EXPOSE 49781
ENTRYPOINT [ "/usr/src/app/BuildBot" ]
 
# Perform a healthcheck.  note that ECS ignores this, so this is for local development
HEALTHCHECK --interval=5s --timeout=2s --retries=3 --start-period=5s CMD [ "/usr/src/app/healthcheck" ]
