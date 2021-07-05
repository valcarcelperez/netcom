del TcpIpCommunicationFramework.*.nupkg

SETLOCAL
SET VERSION=1.1.0

nuget pack cftcpip\TcpIpCommunicationFramework.nuspec -Version %VERSION%
ENDLOCAL
pause