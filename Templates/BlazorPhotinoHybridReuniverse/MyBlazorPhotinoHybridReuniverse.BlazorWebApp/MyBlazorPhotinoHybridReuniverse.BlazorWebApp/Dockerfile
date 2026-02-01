FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY . .

WORKDIR /src/MyBlazorPhotinoHybridReuniverse.BlazorWebApp/MyBlazorPhotinoHybridReuniverse.BlazorWebApp/

ARG TARGETARCH

RUN apt-get update && apt-get install -y python3
RUN dotnet workload install wasm-tools

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a $TARGETARCH -o /app


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app .

USER $APP_UID
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet
ENTRYPOINT ["dotnet", "MyBlazorPhotinoHybridReuniverse.BlazorWebApp.dll"]