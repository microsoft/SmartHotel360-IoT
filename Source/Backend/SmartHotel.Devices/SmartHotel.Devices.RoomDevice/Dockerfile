FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SmartHotel.Devices.RoomDevice/SmartHotel.Devices.RoomDevice.csproj SmartHotel.Devices.RoomDevice/
RUN dotnet restore SmartHotel.Devices.RoomDevice/SmartHotel.Devices.RoomDevice.csproj
COPY . .
WORKDIR /src/SmartHotel.Devices.RoomDevice
RUN dotnet build SmartHotel.Devices.RoomDevice.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartHotel.Devices.RoomDevice.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SmartHotel.Devices.RoomDevice.dll"]
