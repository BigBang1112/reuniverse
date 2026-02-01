FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY . .

WORKDIR /src/MyBlazorPhotinoHybrid.BlazorWasm/

ARG TARGETARCH

RUN apt-get update && apt-get install -y python3
RUN dotnet workload install wasm-tools

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a $TARGETARCH -o /app

    
FROM nginx:alpine
COPY --from=build /app/wwwroot /usr/share/nginx/html