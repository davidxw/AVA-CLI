using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ava
{
    public class AvaDirectMethod
    {
        private const string NAME_COMMAND_TEMPLATE = "{{'@apiVersion':'{0}','name':'{1}'}}";
        private const string SIMPLE_COMMAND_TEMPLATE = "{{'@apiVersion':'{0}'}}";
        private const string LIST_COMMAND_TEMPLATE = "{{'@apiVersion':'{0}', '@query':'{1}'}}";
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

        public async Task<DirectMethodResponse> ExecuteList(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return await Execute();     
            }

            var payload = string.Format(LIST_COMMAND_TEMPLATE, ApiVersion, query);

            return await InvokeMethodWithPayloadAsync(MethodName, payload);
        }

        public async Task<DirectMethodResponse> Execute(FileInfo commandFile)
        {
            if (!File.Exists(commandFile.FullName))
            {
                return new DirectMethodResponse (0, $"Topology file {commandFile.FullName} does not exist");
            }

            var payload = File.ReadAllText(commandFile.FullName);

            return await InvokeMethodWithPayloadAsync(MethodName, payload);
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
