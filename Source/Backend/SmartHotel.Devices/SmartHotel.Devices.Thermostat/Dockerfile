FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SmartHotel.Devices.Thermostat/SmartHotel.Devices.Thermostat.csproj SmartHotel.Devices.Thermostat/
RUN dotnet restore SmartHotel.Devices.Thermostat/SmartHotel.Devices.Thermostat.csproj
COPY . .
WORKDIR /src/SmartHotel.Devices.Thermostat
RUN dotnet build SmartHotel.Devices.Thermostat.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartHotel.Devices.Thermostat.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SmartHotel.Devices.Thermostat.dll"]
