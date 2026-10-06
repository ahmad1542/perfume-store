FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PerfumeStore.App/ PerfumeStore.App/
RUN dotnet restore PerfumeStore.App/PerfumeStore.API/PerfumeStore.API.csproj
RUN dotnet publish PerfumeStore.App/PerfumeStore.API/PerfumeStore.API.csproj -c Release --no-restore -o /app/publish
COPY PerfumeStore.Web/ /app/publish/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PerfumeStore.API.dll"]
