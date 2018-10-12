@echo off
echo ********BEGIN IoT Devices********
pushd "backend/src/SmartHotel.Devices"
docker-compose build
popd
echo ********END IoT Devices********

echo:
echo:

echo ********BEGIN Facility Management Website********
pushd "FacilityManagementWebsite/SmartHotel.FacilityManagementWeb"
docker-compose build
popd
echo ********END Facility Management Website********

echo:
echo:

echo ********BEGIN APIs********
pushd "backend/src/SmartHotel.Services"
docker-compose build
popd
echo ********END APIs********

echo:
echo:

echo Build complete, press any key to exit.
pause
@echo on
exit /b