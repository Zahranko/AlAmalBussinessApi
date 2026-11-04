# syntax=docker/dockerfile:1.7
# AlAmalBusiness API: production image for the VPS (built by .github/workflows/deploy-vps.yml).
#
# - Multi-stage: the SDK (~900 MB) never ships; the final image is the ASP.NET
#   runtime on Ubuntu "chiseled" (no shell, no package manager, non-root user).
# - "-extra" variant: keeps ICU + tzdata. Arabic text, culture-aware
#   formatting and ClosedXML need real globalization; don't switch to plain
#   chiseled or InvariantGlobalization.
# - Restore is its own layer keyed only on the .csproj files, so a code-only
#   change rebuilds in seconds; NuGet packages are also kept in a BuildKit cache.

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
WORKDIR /src

COPY AlAmalBusiness.sln ./
COPY AlAmalBusiness.Api/AlAmalBusiness.Api.csproj AlAmalBusiness.Api/
COPY AlAmalBusiness.Application/AlAmalBusiness.Application.csproj AlAmalBusiness.Application/
COPY AlAmalBusiness.Domain/AlAmalBusiness.Domain.csproj AlAmalBusiness.Domain/
COPY AlAmalBusiness.Infrastructure/AlAmalBusiness.Infrastructure.csproj AlAmalBusiness.Infrastructure/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore AlAmalBusiness.Api/AlAmalBusiness.Api.csproj -r linux-x64 -p:PublishReadyToRun=true

COPY . .
# ReadyToRun: precompiled code, so the app starts and serves its first
# requests fast instead of JIT-compiling everything (tiered PGO still
# optimises the hot paths afterwards).
# Server GC + concurrent GC: the csproj pins Workstation/non-concurrent for the
# old 1 GB SmarterASP pool. On the VPS the container has its own CPUs and
# memory, so Server GC (with .NET's DATAS, which sizes the heap to the load)
# gives more throughput and shorter pauses. Overridden here only, so the
# SmarterASP build is unchanged.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish AlAmalBusiness.Api/AlAmalBusiness.Api.csproj \
        -c Release -r linux-x64 --self-contained false --no-restore \
        -p:PublishReadyToRun=true \
        -p:UseAppHost=false \
        -p:ServerGarbageCollection=true \
        -p:ConcurrentGarbageCollection=true \
        -p:DebugType=none \
        -o /app \
 && rm -f /app/appsettings.Development.json \
 && mkdir -p /dpkeys

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra AS final
WORKDIR /app
# Data Protection keys live on a volume so they survive restarts and are
# shared by the blue and green containers. The folder must exist (owned by the
# app user) in the image, so a new named volume inherits that owner.
COPY --from=build --chown=1654:1654 /dpkeys /home/app/.aspnet/DataProtection-Keys
# App files stay root-owned and read-only to the app user.
COPY --from=build /app ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_TieredPGO=1
# DOTNET_EnableDiagnostics=0: no debugger/profiler IPC sockets, which also lets
# the container run with a read-only root filesystem.

EXPOSE 8080
USER 1654
ENTRYPOINT ["dotnet", "AlAmalBusiness.Api.dll"]
