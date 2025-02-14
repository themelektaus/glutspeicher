FROM mcr.microsoft.com/dotnet/sdk:9.0

EXPOSE 80

ENV TZ=Europe/Vienna

WORKDIR /app

COPY "Glutspeicher Server/Build" ./

ENTRYPOINT [ "./Glutspeicher Server" ]
