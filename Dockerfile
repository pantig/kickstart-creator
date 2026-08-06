# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/KickstartCreator.Web/KickstartCreator.Web.csproj src/KickstartCreator.Web/
RUN dotnet restore src/KickstartCreator.Web/KickstartCreator.Web.csproj

COPY src/KickstartCreator.Web/ src/KickstartCreator.Web/
RUN dotnet publish src/KickstartCreator.Web/KickstartCreator.Web.csproj \
      -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# xorriso/dosfstools/mtools build the OEMDRV ISO+IMG (see
# Services/CliRemovableMediaBuilder.cs); openssl produces the SHA-512 crypt
# password hashes (see Services/OpenSslShaCryptHashingService.cs); tzdata
# backs the IANA timezone cross-check (see Services/SystemTimeZoneCatalog.cs);
# wget is used only by the compose healthcheck. GRUB's PBKDF2 hash is produced
# natively in .NET, so no grub2-tools dependency here.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        xorriso \
        dosfstools \
        mtools \
        openssl \
        tzdata \
        wget \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r kickstart \
    && useradd -r -g kickstart -d /app kickstart \
    && mkdir -p /data/artifacts

COPY --from=build /app/publish .

RUN chown -R kickstart:kickstart /app /data/artifacts

ENV ASPNETCORE_URLS=http://+:8080 \
    ArtifactStorage__RootPath=/data/artifacts

EXPOSE 8080
VOLUME ["/data/artifacts"]

USER kickstart
ENTRYPOINT ["dotnet", "KickstartCreator.Web.dll"]
