FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SmartHotel.Services.FacilityManagement/SmartHotel.Services.FacilityManagement.csproj SmartHotel.Services.FacilityManagement/
RUN dotnet restore SmartHotel.Services.FacilityManagement/SmartHotel.Services.FacilityManagement.csproj
COPY . .
WORKDIR /src/SmartHotel.Services.FacilityManagement
RUN dotnet build SmartHotel.Services.FacilityManagement.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartHotel.Services.FacilityManagement.csproj -c Release -o /app


FROM base AS final
WORKDIR /app
COPY --from=publish /app .

RUN ls /app

ENTRYPOINT ["dotnet", "SmartHotel.Services.FacilityManagement.dll"]
