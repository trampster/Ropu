# Ropu
Group PPT System.

## Linux 18.04 Setup

* Install dotnet core sdk https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/sdk-current

## Build and Run Core
* cd Bender
* dotnet run --configuration Release -- core.json

## Build and Run Client 
* cd ClientUI
* dotnet run --configuration Release -n {unit-id} -b {Core IP Address}

## Build Snap
* dotnet publish -c Release -o Install --self-contained -r linux-x64
* chmod 775 ClientUI/Install/ClientUI
* snapcraft
* snap install --devmode ropu_0.1_amd64.snap