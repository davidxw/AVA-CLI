using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace ava
{
    class Program
    {
        const string CONNECTION_SETTINGS_FILENAME = "connection.json";

        const string GRAPH_TOPOLOGY_LABEL = "Graph topology";
        const string GRAPH_INSTANCE_LABEL = "Graph instance";

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            // TODO - just show help if no subcommands are provided

            // connect

            var connectCommand = new Command("connect");

            var ca1 = new Argument<string>("connectionString", "A connection string for the IoTHub");
            connectCommand.AddArgument(ca1);

            var ca2 = new Argument<string>("deviceId", "The IoT Edge device Id");
            connectCommand.AddArgument(ca2);

            var ca3 = new Argument<string>("moduleId", "The AVA module Id");
            connectCommand.AddArgument(ca3);

            connectCommand.Handler = CommandHandler.Create<string, string, string>(connectCommandHandler);

            rootCommand.Add(connectCommand);

            // ######### topology

            var topologyCommand = new Command("topology");

            // TODO - just show help if no subcommands are provided

            rootCommand.Add(topologyCommand);

            // ## topology list 

            var topologyListCommand = new Command("list");
            topologyListCommand.Handler = CommandHandler.Create(topologyListCommandHandler);

            topologyCommand.Add(topologyListCommand);

            // ## topology get 

            var topologyGetCommand = new Command("get");

            var tga1 = new Argument<string>("topologyName", "The name of the topology to get");
            topologyGetCommand.AddArgument(tga1);

            topologyGetCommand.Handler = CommandHandler.Create<string>(topologyGetCommandHandler);

            topologyCommand.Add(topologyGetCommand);

            // ## topology set 

            var topologySetCommand = new Command("set");

            var tsa1 = new Argument<FileInfo>("topologyFile", "A file containing full topology specification");
            topologySetCommand.AddArgument(tsa1);

            topologySetCommand.Handler = CommandHandler.Create<FileInfo>(topologySetCommandHandler);

            topologyCommand.Add(topologySetCommand);

            // ## topology delete 

            var topologyDeleteCommand = new Command("delete");

            var tda1 = new Argument<string>("topologyName", "The name of the topology to delete");
            topologyDeleteCommand.AddArgument(tda1);

            topologyDeleteCommand.Handler = CommandHandler.Create<string>(topologyDeleteCommandHandler);

            topologyCommand.Add(topologyDeleteCommand);

            // ######### instance 

            var instanceCommand = new Command("instance");

            // TODO - just show help if no subcommands are provided

            rootCommand.Add(instanceCommand);

            // ## instance list 

            var instanceListCommand = new Command("list");
            instanceListCommand.Handler = CommandHandler.Create(instanceListCommandHandler);

            instanceCommand.Add(instanceListCommand);

            // ## instance get 

            var instanceGetCommand = new Command("get");

            var iga1 = new Argument<string>("instanceName", "The name of the instance to get");
            instanceGetCommand.AddArgument(iga1);

            instanceGetCommand.Handler = CommandHandler.Create<string>(instanceGetCommandHandler);

            instanceCommand.Add(instanceGetCommand);

            // ## instance set 

            var instanceSetCommand = new Command("set");

            var isa1 = new Argument<string>("instanceName", "The name of the instance to set");
            instanceSetCommand.AddArgument(isa1);

            var isa2 = new Argument<string>("topologyName", "The name of the topology to use for the instance");
            instanceSetCommand.AddArgument(isa2);

            var iso1 = new Option<string>("--paramater", "A paramater to set on the instaince in the format 'paramName=paramValue'", ArgumentArity.ZeroOrMore);
            iso1.AddAlias("-p");
            instanceSetCommand.AddOption(iso1);

            instanceSetCommand.Handler = CommandHandler.Create<string, string, string[]>(instanceSetCommandHandler);

            instanceCommand.Add(instanceSetCommand);

            // ## instance activate 

            var instanceActivateCommand = new Command("activate");

            var iaa1 = new Argument<string>("instanceName", "The name of the instance to activate");
            instanceActivateCommand.AddArgument(iaa1);

            instanceActivateCommand.Handler = CommandHandler.Create<string>(instanceActivateCommandHandler);

            instanceCommand.Add(instanceActivateCommand);

            // ## instance deactivate 

            var instanceDeactivateCommand = new Command("deactivate");

            var ida1 = new Argument<string>("instanceName", "The name of the instance to deactivate");
            instanceDeactivateCommand.AddArgument(ida1);

            instanceDeactivateCommand.Handler = CommandHandler.Create<string>(instanceDeactivateCommandHandler);

            instanceCommand.Add(instanceDeactivateCommand);

            // ## instance delete 

            var instanceDeleteCommand = new Command("delete");

            var idea1 = new Argument<string>("instanceName", "The name of the instance to delete");
            instanceDeleteCommand.AddArgument(idea1);

            instanceDeleteCommand.Handler = CommandHandler.Create<string>(instanceDeleteCommandHandler);

            instanceCommand.Add(instanceDeleteCommand);

            // Parse the incoming args and invoke the required handler
            var result = await rootCommand.InvokeAsync(args);

            return result;
        }

        private static void connectCommandHandler(string connectionString, string deviceId, string moduleId)
        {
            var connectionSettings = new ConnectionSettings { IoTHubConnectionString = connectionString, DeviceId = deviceId, ModuleId = moduleId };

            File.WriteAllText(CONNECTION_SETTINGS_FILENAME, JsonConvert.SerializeObject(connectionSettings));

            Console.WriteLine("Connection details saved");
        }

        private async static Task topologyListCommandHandler()
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphTopologyList");
            var output = await command.Execute();

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_TOPOLOGY_LABEL);
            }
            else
            {
                var f = "{0,-30} {1,-30}";

                Console.WriteLine(String.Format(f, "Name", "Date Created (UTC)"));
                foreach (var t in output.ResponseBody.value)
                {
                    Console.WriteLine(String.Format(f, t.name, t.systemData.createdAt));
                }
            }
        }

        private async static Task topologyGetCommandHandler(string topologyName)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphTopologyGet");
            var output = await command.Execute(topologyName);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_TOPOLOGY_LABEL, topologyName);
            }
            else
            {
                Console.WriteLine(output.ResponseBodyString);
            }
        }

        private async static Task topologySetCommandHandler(FileInfo topologyFile)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphTopologySet");
            var output = await command.Execute(topologyFile);

            WriteResult(output, GRAPH_TOPOLOGY_LABEL);
        }

        private async static Task topologyDeleteCommandHandler(string topologyName)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphTopologyDelete");
            var output = await command.Execute(topologyName);

            WriteResult(output, GRAPH_TOPOLOGY_LABEL, topologyName, "deleted", null, "is being referenced by more than one graph instance and cannot be deleted");
        }

        private async static Task instanceListCommandHandler()
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphInstanceList");
            var output = await command.Execute();

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_INSTANCE_LABEL);
            }
            else
            {
                var f = "{0,-30} {1,-30} {2, -10} {3, -30}";

                Console.WriteLine(String.Format(f, "Name", "Date Created (UTC)", "State", "Topology"));
                foreach (var t in output.ResponseBody.value)
                {
                    Console.WriteLine(String.Format(f, t.name, t.systemData.createdAt, t.properties.state, t.properties.topologyName));
                }
            }
        }

        private async static Task instanceGetCommandHandler(string instanceName)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphInstanceGet");
            var output = await command.Execute(instanceName);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName);
            }
            else
            {
                Console.WriteLine(output.ResponseBodyString);
            }
        }

        private async static Task instanceSetCommandHandler(string instanceName, string topologyName, string[] paramater)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphInstanceSet");
            var output = await command.Execute(instanceName, topologyName, paramater);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, null, null, " already exists.");

            if (output.IsSuccess)
            {
                Console.WriteLine(output.ResponseBodyString);
            }
        }

        private async static Task instanceActivateCommandHandler(string instanceName)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphInstanceActivate");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "activated");
        }

        private async static Task instanceDeactivateCommandHandler(string instanceName)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphInstanceDeactivate");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "deactivated");
        }

        private async static Task instanceDeleteCommandHandler(string instanceName)
        {
            var connection = GetConnectionSettings();

            if (connection == null) return;

            var command = new AvaCommand(connection, "GraphInstanceDelete");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "deleted", null, $"is in an active state and cannot be deleted. Run 'ava instance deactivate {instanceName}' to decativate.");
        }

        private static ConnectionSettings GetConnectionSettings()
        {
            ConnectionSettings connectionSettings = null;

            try
            {
                var connectSettingsFileContent = File.ReadAllText(CONNECTION_SETTINGS_FILENAME);

                if (string.IsNullOrEmpty(connectSettingsFileContent))
                {
                    throw(new Exception());
                }

                connectionSettings = JsonConvert.DeserializeObject<ConnectionSettings>(connectSettingsFileContent);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Could not load connection details. Run 'lva connect'");
                Console.ResetColor();
            }

            return connectionSettings;
        }

        private static void WriteResult(DirectMethodResponse output, string entity, string instanceName = null, string action200 = "updated", string action201 = "created", string message409 = null)
        {
            instanceName = string.IsNullOrEmpty(instanceName) ? null : $" {instanceName}";

            switch (output.ResponseCode)
            {
                case 0:
                    Console.WriteLine($"Operation failed - {output.ResponseMessage}");
                    break;
                case 200:
                    Console.WriteLine($"{entity}{instanceName} {action200}.");
                    break;
                case 201:
                    Console.WriteLine($"{entity}{instanceName} {action201}.");
                    break;
                case 204:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{entity}{instanceName} does not exist");
                    Console.ResetColor();
                    break;
                case 409:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{entity}{instanceName} {message409}");
                    Console.ResetColor();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Operation failed - response code {output.ResponseCode}");
                    Console.ResetColor();
                    Console.WriteLine(output.ResponseBodyString);
                    break;
            }
        }
    }
}
