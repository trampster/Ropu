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