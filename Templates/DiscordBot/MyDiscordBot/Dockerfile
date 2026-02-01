FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src
COPY . .

WORKDIR /src/MyDiscordBot/

ARG TARGETARCH

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} -o /app


FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache \
    icu-data-full \
    icu-libs

WORKDIR /app

COPY --from=build /app .

USER $APP_UID
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet
ENTRYPOINT ["dotnet", "MyDiscordBot.dll"]