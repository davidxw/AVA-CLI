using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;

namespace ava
{
    public class AvaDirectMethod
    {
        private const string NAME_COMMAND_TEMPLATE = "{{'@apiVersion':'{0}','name':'{1}'}}";
        private const string SIMPLE_COMMAND_TEMPLATE = "{{'@apiVersion':'{0}'}}";
        private const string INSTANCE_SET_COMMAND_TEMPLATE = "{{'@apiVersion':'{0}','name':'{1}','properties':{{'topologyName':'{2}','parameters':[{3}]}}}}";

        private ConnectionSettings connectionSettings;

        public string MethodName { get; set; }

        public string ApiVersion { get; set; }

        public AvaDirectMethod(ConnectionSettings connectionSettings, string methodName)
        {
            this.connectionSettings = connectionSettings;
            this.MethodName = methodName;
            this.ApiVersion = "1.0";
        }

        public async Task<DirectMethodResponse> Execute()
        {
            var payload = string.Format(SIMPLE_COMMAND_TEMPLATE, ApiVersion);

            return await InvokeMethodWithPayloadAsync(MethodName, payload);
        }

        public async Task<DirectMethodResponse> Execute(string nameProperty)
        {
            var payload = string.Format(NAME_COMMAND_TEMPLATE, ApiVersion, nameProperty);

            return await InvokeMethodWithPayloadAsync(MethodName, payload);
        }

        public async Task<DirectMethodResponse> Execute(string nameProperty, string instanceNameProperty, string[] paramaters)
        {
            var paramCollection = new List<string>();

            foreach (var paramater in new List<string>(paramaters))
            {
                var paramaterPair = paramater.Split("=");

                if (paramaterPair.Length != 2)
                {
                    return new DirectMethodResponse (0, "Paramater list is invalid - paramaters should be expressed in the format 'paramName=paramValue'");
                }

                paramCollection.Add($"{{'name':'{paramaterPair[0]}','value':'{paramaterPair[1]}'}}");
            }

            var paramString = string.Join(",", paramCollection);

            var payload = string.Format(INSTANCE_SET_COMMAND_TEMPLATE, ApiVersion, nameProperty, instanceNameProperty, paramString);

            return await InvokeMethodWithPayloadAsync(MethodName, payload);
        }

        public async Task<DirectMethodResponse> Execute(FileInfo topologyFile, string topologyName)
        {
            if (!File.Exists(topologyFile.FullName))
            {
                return new DirectMethodResponse (0, $"Topology file {topologyFile.FullName} does not exist");
            }

            var payload = File.ReadAllText(topologyFile.FullName);

            // load payload into dynamic object so we can query the topology name, as well as override
            dynamic topologyFileJson;

            try
            {
                topologyFileJson = JsonConvert.DeserializeObject<ExpandoObject>(payload);
            }
            catch (Exception ex)
            {
                return new DirectMethodResponse(0, $"Failed to parse topology file {topologyFile.FullName} - {ex.Message}");
            }

            if (!string.IsNullOrEmpty(topologyName))
            {
                topologyFileJson.name = topologyName;
                payload = JsonConvert.SerializeObject(topologyFileJson);
            }

            var output = await InvokeMethodWithPayloadAsync(MethodName, payload);

            output.EntityName = topologyFileJson.name;

            return output;
        }

        private async Task<DirectMethodResponse> InvokeMethodWithPayloadAsync(string methodName, string payload)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionSettings.IoTHubConnectionString);

            // Create a direct method call
            var methodInvocation = new CloudToDeviceMethod(methodName);

            methodInvocation.ResponseTimeout = TimeSpan.FromSeconds(30);
            methodInvocation.SetPayloadJson(payload);

            try
            {
                var response = await serviceClient.InvokeDeviceMethodAsync(connectionSettings.DeviceId, connectionSettings.ModuleId, methodInvocation);
                
                var responseString = response.GetPayloadAsJson();

                return new DirectMethodResponse(response.Status, responseString);
            }
            catch (Exception ex)
            {
                return new DirectMethodResponse(0, ex.Message);
            }
        }
    }
}
