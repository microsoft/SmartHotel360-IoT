FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SmartHotel.Devices.Light/SmartHotel.Devices.Light.csproj SmartHotel.Devices.Light/
RUN dotnet restore SmartHotel.Devices.Light/SmartHotel.Devices.Light.csproj
COPY . .
WORKDIR /src/SmartHotel.Devices.Light
RUN dotnet build SmartHotel.Devices.Light.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartHotel.Devices.Light.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SmartHotel.Devices.Light.dll"]
