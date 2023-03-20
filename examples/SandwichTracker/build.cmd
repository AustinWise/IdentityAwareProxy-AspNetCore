@echo off
setlocal

pushd %~dp0

if "%1" == "" goto wrong_number_of_args
set __ImageTag=%1
shift
if NOT "%1" == "" goto wrong_number_of_args


dotnet publish --os linux --arch x64 /t:PublishContainer -c Release -p:ContainerImageTag=%__ImageTag%
if NOT '%ERRORLEVEL%' == '0' goto exit_with_error
docker push us-central1-docker.pkg.dev/test-iap-379718/sandwich-apps/sandwichtracker:%__ImageTag%
if NOT '%ERRORLEVEL%' == '0' goto exit_with_error

exit /b 0

:wrong_number_of_args
echo Wrong number of arguments, expected exactly one for the image tag name.
exit /b 1

:exit_with_error
exit /b 1
