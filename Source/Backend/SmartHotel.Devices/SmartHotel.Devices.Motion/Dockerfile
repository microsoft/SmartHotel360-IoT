FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SmartHotel.Devices.Motion/SmartHotel.Devices.Motion.csproj SmartHotel.Devices.Motion/
RUN dotnet restore SmartHotel.Devices.Motion/SmartHotel.Devices.Motion.csproj
COPY . .
WORKDIR /src/SmartHotel.Devices.Motion
RUN dotnet build SmartHotel.Devices.Motion.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartHotel.Devices.Motion.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SmartHotel.Devices.Motion.dll"]
