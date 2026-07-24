FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /codigo

COPY ["BPCoreApi/BPCoreApi.csproj", "BPCoreApi/"]
RUN dotnet restore "BPCoreApi/BPCoreApi.csproj"

COPY . .
WORKDIR /codigo/BPCoreApi
RUN dotnet publish "BPCoreApi.csproj" \
    --configuration Release \
    --output /publicacion \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS ejecucion
WORKDIR /aplicacion

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=compilacion /publicacion .

USER app
ENTRYPOINT ["dotnet", "BPCoreApi.dll"]
