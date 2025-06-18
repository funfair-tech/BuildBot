# chiseled-extra is needed over chiseled for Discord
#FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-noble-chiseled-extra
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-preview-noble-chiseled-extra

WORKDIR /usr/src/app

# Bundle App Source
COPY BuildBot .
COPY appsettings.json .

EXPOSE 8080
ENTRYPOINT [ "/usr/src/app/BuildBot" ]
 
# Perform a healthcheck.  note that ECS ignores this, so this is for local development
HEALTHCHECK --interval=5s --timeout=2s --retries=3 --start-period=5s CMD [ "/usr/src/app/BuildBot", "--health-check", "http://127.0.0.1:8080/ping?source=docker" ]
