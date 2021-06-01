# AVA CLI

![Build and Release](https://github.com/davidxw/ava-cli/actions/workflows/dotnet.yml/badge.svg)

A simple CLI for managing pipeline topologies and pipelines in Azure Video Analyzer. The CLI implements the AVA module direct commands described in the AVA direct methods [docuemntation page](https://docs.microsoft.com/en-us/azure/azure-video-analyzer/video-analyzer-docs/direct-methods).


## Usage

ava \<command(s)\> \<parameters(s)\> \<options(s)\>

### Commands

connection set \<connectionString\> \<deviceId\> \<moduleId\>  
connection clear

topology list 
topology get \<topologyName\>  
topology set \<toplogyFilePath\>  
topology delete \<topologyName\>

pipeline list 
pipeline get \<intanceName\>  
pipeline set \<intanceName\> \<topologyName\> -p paramName1=paramValue1  
pipeline delete \<intanceName\>  
pipeline activate \<intanceName\>  
pipeline deactivate \<intanceName\> 

For all of the topology and pipeline commands, using options -c \<connectionString\>, -d \<deviceId\> and/or -m \<moduleId\> to override the device and module Id specified in the default connection. This can be useful if you need to script AVA commands accross a number of devices, or if you don't want to persist connection settings using ava connection set.

### Examples

Set up a connection to Edge device 'edgeDevice1', AVA module 'avaedge':

`ava connection set <connectionString> edgeDevice1 avaedge`

Create topology from file topology.json:

`ava topology set topology.json`

Create topology from file topology.json, but override the topology name in the file with 'topName':

`ava topology set topology.json -n topName`

Create a pipeline called 'rtsp-sim' using topology 'topName', and set parameter 'rtspUrl':

`ava pipeline set rtsp-sim topName -p  -p "rtspUrl=rtsp://rtspsim:554/media/windows.mkv"`

Activate the pipeline 'rtsp-sim':

`ava pipeline activate rtsp-sim`

Create topology from file topology.json on device 'edgeDevice2' (using the previously set connection string and AVA module name):

`ava topology set topology.json -d edgeDevice2`



## Installation
* Download the latst release from the [releases](https://github.com/davidxw/AVA-CLI/releases) page
* Extract the files from the release into a folder, and create a PATH to the folder
* (a more automated release process coming soon!!!!)