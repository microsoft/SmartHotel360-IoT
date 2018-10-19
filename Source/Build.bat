@echo off
echo ********BEGIN IoT Devices********
pushd "Backend/SmartHotel.Devices"
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
pushd "Backend/SmartHotel.Services"
docker-compose build
popd
echo ********END APIs********

echo:
echo:

echo Build complete, press any key to exit.
pause
@echo on
exit /b