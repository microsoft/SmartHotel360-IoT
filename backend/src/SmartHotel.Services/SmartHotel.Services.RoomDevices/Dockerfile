FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SmartHotel.Services.RoomDevices/SmartHotel.Services.RoomDevices.csproj SmartHotel.Services.RoomDevices/
RUN dotnet restore SmartHotel.Services.RoomDevices/SmartHotel.Services.RoomDevices.csproj
COPY . .
WORKDIR /src/SmartHotel.Services.RoomDevices
RUN dotnet build SmartHotel.Services.RoomDevices.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartHotel.Services.RoomDevices.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SmartHotel.Services.RoomDevices.dll"]
