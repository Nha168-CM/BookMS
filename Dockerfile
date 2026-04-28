FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BookMS/BookMS.csproj", "BookMS/"]
RUN dotnet restore "BookMS/BookMS.csproj"
COPY . .
WORKDIR "/src/BookMS"
RUN dotnet build "BookMS.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookMS.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookMS.dll"]
