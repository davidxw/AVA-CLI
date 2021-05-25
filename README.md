# AVA CLI

![Build and Release](https://github.com/davidxw/ava-cli/actions/workflows/dotnet.yml/badge.svg)

A simple CLI for managing graph topologies and graph instances in Azure Video Analytics. The CLI implements the AVA module direct commands described in the AVA direct methods [docuemntation page](https://docs.microsoft.com/en-us/azure/media-services/live-video-analytics-edge/direct-methods).


## Usage

ava \<command(s)\> \<parameters(s)\> \<options(s)\>

### Commands

connection set \<connectionString\> \<deviceId\> \<moduleId\>  
connection clear

topology list 
topology get \<topologyName\>  
topology set \<toplogyFilePath\>  
topology delete \<topologyName\>

instance list 
instance get \<intanceName\>  
instance set \<intanceName\> \<topologyName\> -p paramName1=paramValue1  
instance delete \<intanceName\>  
instance activate \<intanceName\>  
instance deactivate \<intanceName\> 

For all of the topology and instance commands, using option -d \<deviceId\> and/or -m \<moduleId\> to override the device and module Id specified in the default connection. This can be useful if you need to script AVA commands accross a number of devices.

## Installation
* Download the latst release from the [releases](https://github.com/davidxw/AVA-CLI/releases) page
* Extract the files from the release into a folder, and create a PATH to the folder
* (a more automated release process coming soon!!!!)