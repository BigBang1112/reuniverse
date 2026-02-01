FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

RUN apk add --no-cache clang build-base zlib-dev

WORKDIR /src
COPY . .

WORKDIR /src/MyGenericHost/

ARG TARGETARCH

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} -o /app


FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app

COPY --from=build /app .

USER $APP_UID
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet
ENTRYPOINT ["./MyGenericHost"]