using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace ava
{
    class Program
    {
        const string ROOT_COMMAND_NAME = "ava";

        const string CONNECTION_SETTINGS_FILENAME = "connection.json";

        const string GRAPH_TOPOLOGY_LABEL = "Graph topology";
        const string GRAPH_INSTANCE_LABEL = "Graph instance";

        static List<string> CONNECTIONS_REQUIRED = new List<String> { "topology", "instance" };

        static ConnectionSettings _connectionSettings = null;

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var globalOptionDeviceId = new Option<string>("--deviceId", "Override the device Id in connection settings", ArgumentArity.ExactlyOne);
            globalOptionDeviceId.AddAlias("-d");

            rootCommand.AddGlobalOption(globalOptionDeviceId);

            var globalOptionModuleId = new Option<string>("--moduleId", "Override the module Id in connection settings", ArgumentArity.ExactlyOne);
            globalOptionModuleId.AddAlias("-m");

            rootCommand.AddGlobalOption(globalOptionModuleId);

            rootCommand.Handler = CommandHandler.Create(rootCommandHandler);

            // connection

            var connectCommand = new Command("connection");

            rootCommand.Add(connectCommand);

            // connection set

            var connectionSetCommand = new Command("set");

            var ca1 = new Argument<string>("connectionString", "A connection string for the IoTHub");
            connectionSetCommand.AddArgument(ca1);

            var ca2 = new Argument<string>("deviceId", "The IoT Edge device Id");
            connectionSetCommand.AddArgument(ca2);

            var ca3 = new Argument<string>("moduleId", "The AVA module Id");
            connectionSetCommand.AddArgument(ca3);

            connectionSetCommand.Handler = CommandHandler.Create<string, string, string>(connectionSetCommandHandler);

            connectCommand.Add(connectionSetCommand);

            // connection clear
            var connectionClearCommand = new Command("clear");
            connectionClearCommand.Handler = CommandHandler.Create(connectionClearCommandHandler);

            connectCommand.Add(connectionClearCommand);

            // ######### topology

            var topologyCommand = new Command("topology");

            // TODO - just show help if no subcommands are provided

            rootCommand.Add(topologyCommand);

            // ## topology list 

            var topologyListCommand = new Command("list");

            //var tgo = new Option<string>("--query", "Optionally apply an ODATA query to the results", ArgumentArity.ZeroOrOne);
            //tgo.AddAlias("-q");

            //topologyListCommand.AddOption(tgo);

            topologyListCommand.Handler = CommandHandler.Create<string>(topologyListCommandHandler);

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

            //var ilo = new Option<string>("--query", "Optionally apply an ODATA query to the results", ArgumentArity.ZeroOrOne);
            //ilo.AddAlias("-q");

            //instanceListCommand.AddOption(ilo);

            instanceListCommand.Handler = CommandHandler.Create<string>(instanceListCommandHandler);

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


            var clb = new CommandLineBuilder(rootCommand);
            clb.UseDefaults();
            clb.ResponseFileHandling = System.CommandLine.Parsing.ResponseFileHandling.Disabled;

            clb.UseMiddleware(async (context, next) =>
            {
                if (context.ParseResult.Tokens.Count >= 2 && CONNECTIONS_REQUIRED.Contains(context.ParseResult.Tokens[0].Value))
                {
                    // a connection is required - get connection settings and only proceed if settings are valid
                    _connectionSettings = GetConnectionSettings();

                    if (_connectionSettings != null)
                    {
                        if (context.ParseResult.HasOption("-d") && !string.IsNullOrEmpty((string)context.ParseResult.ValueForOption("-d")))
                        {
                            _connectionSettings.DeviceId = (string)context.ParseResult.ValueForOption("-d");
                        }

                        if (context.ParseResult.HasOption("-m") && !string.IsNullOrEmpty((string)context.ParseResult.ValueForOption("-m")))
                        {
                            _connectionSettings.ModuleId = (string)context.ParseResult.ValueForOption("-m");
                        }

                        await next(context);
                    }
                }
                else
                {
                    await next(context);
                }
            });

            var parser = clb.Build();

            // Parse the incoming args and invoke the required handler
            var result = await parser.InvokeAsync(args);

            return result;
        }

        private static void connectionSetCommandHandler(string connectionString, string deviceId, string moduleId)
        {
            var connectionSettings = new ConnectionSettings { IoTHubConnectionString = connectionString, DeviceId = deviceId, ModuleId = moduleId };

            File.WriteAllText(GetConnectionSettingsFilePath(), JsonConvert.SerializeObject(connectionSettings));

            Console.WriteLine("Connection details saved");
        }

        private static void connectionClearCommandHandler()
        {
            File.Delete(GetConnectionSettingsFilePath());

            Console.WriteLine("Connection details cleared");
        }

        private async static Task topologyListCommandHandler(string query)
        {
            var command = new AvaCommand(_connectionSettings, "GraphTopologyList");
            var output = await command.ExecuteList(query);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_TOPOLOGY_LABEL);
            }
            else
            {
                var f = "{0,-30} {1,-30}";

                Console.WriteLine(String.Format(f, "Name", "Date Created (UTC)"));
                try
                {
                    foreach (var t in output.ResponseBody.value)
                    {
                        Console.WriteLine(String.Format(f, t.name, t.systemData.createdAt));
                    }
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to parse response from AVA. The raw response received was:");
                    Console.ResetColor();
                    Console.WriteLine(output.ResponseBodyString);
                }
            }
        }

        private async static Task topologyGetCommandHandler(string topologyName)
        {
            var command = new AvaCommand(_connectionSettings, "GraphTopologyGet");
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
            var command = new AvaCommand(_connectionSettings, "GraphTopologySet");
            var output = await command.Execute(topologyFile);

            WriteResult(output, GRAPH_TOPOLOGY_LABEL);
        }

        private async static Task topologyDeleteCommandHandler(string topologyName)
        {
            var command = new AvaCommand(_connectionSettings, "GraphTopologyDelete");
            var output = await command.Execute(topologyName);

            WriteResult(output, GRAPH_TOPOLOGY_LABEL, topologyName, "deleted", null, "is being referenced by more than one graph instance and cannot be deleted");
        }

        private async static Task instanceListCommandHandler(string query)
        {
            var command = new AvaCommand(_connectionSettings, "GraphInstanceList");
            var output = await command.ExecuteList(query);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_INSTANCE_LABEL);
            }
            else
            {
                var f = "{0,-30} {1,-30} {2, -10} {3, -30}";

                Console.WriteLine(String.Format(f, "Name", "Date Created (UTC)", "State", "Topology"));

                try
                {
                    foreach (var t in output.ResponseBody.value)
                    {
                        Console.WriteLine(String.Format(f, t.name, t.systemData.createdAt, t.properties.state, t.properties.topologyName));
                    }
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to parse response from AVA. The raw response received was:");
                    Console.ResetColor();
                    Console.WriteLine(output.ResponseBodyString);
                }
            }
        }

        private async static Task instanceGetCommandHandler(string instanceName)
        {
            var command = new AvaCommand(_connectionSettings, "GraphInstanceGet");
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
            var command = new AvaCommand(_connectionSettings, "GraphInstanceSet");
            var output = await command.Execute(instanceName, topologyName, paramater);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "updated", "created", " already exists.");

            if (output.IsSuccess)
            {
                Console.WriteLine(output.ResponseBodyString);
            }
        }

        private async static Task instanceActivateCommandHandler(string instanceName)
        {
            var command = new AvaCommand(_connectionSettings, "GraphInstanceActivate");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "activated");
        }

        private async static Task instanceDeactivateCommandHandler(string instanceName)
        {
            var command = new AvaCommand(_connectionSettings, "GraphInstanceDeactivate");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "deactivated");
        }

        private async static Task instanceDeleteCommandHandler(string instanceName)
        {
            var command = new AvaCommand(_connectionSettings, "GraphInstanceDelete");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "deleted", null, $"is in an active state and cannot be deleted. Run '{ROOT_COMMAND_NAME} instance deactivate {instanceName}' to decativate.");
        }

        private static ConnectionSettings GetConnectionSettings()
        {
            ConnectionSettings connectionSettings = null;

            try
            {
                var connectSettingsFileContent = File.ReadAllText(GetConnectionSettingsFilePath());

                if (string.IsNullOrEmpty(connectSettingsFileContent))
                {
                    throw (new Exception());
                }

                connectionSettings = JsonConvert.DeserializeObject<ConnectionSettings>(connectSettingsFileContent);

                if (string.IsNullOrEmpty(connectionSettings.IoTHubConnectionString) || string.IsNullOrEmpty(connectionSettings.DeviceId) || string.IsNullOrEmpty(connectionSettings.ModuleId))
                {
                    throw (new Exception());
                }
            }
            catch (Exception)
            {
                connectionSettings = null;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"No connection details have been specified. Run '{ROOT_COMMAND_NAME} connect'");
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
                    if (!string.IsNullOrEmpty(output.ResponseBody))
                        Console.WriteLine(output.ResponseBodyString);
                    break;
            }
        }

        private static string GetConnectionSettingsFilePath()
        {
            var connectionSettingFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ava-cli");

            Directory.CreateDirectory(connectionSettingFileDir);

            return Path.Combine(connectionSettingFileDir, CONNECTION_SETTINGS_FILENAME);
        }


        private static void rootCommandHandler()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("AVA CLI - a simple CLI for Azure Video Analytics");
            Console.ResetColor();
            Console.WriteLine();

            var connectionSettings = GetConnectionSettings();

            if (connectionSettings != null)
            {
                Console.WriteLine("Current connection settings:");
                Console.WriteLine($"  IoT Hub:   {connectionSettings.IoTHubConnectionString}");
                Console.WriteLine($"  Device Id: {connectionSettings.DeviceId}");
                Console.WriteLine($"  Module Id: {connectionSettings.ModuleId}");
            }

            Console.WriteLine();

            Console.WriteLine("Commands:");
            Console.WriteLine("  connection set <connectionString> <deviceId> <moduleId>");
            Console.WriteLine("  connection clear");
            Console.WriteLine("  topology list");
            Console.WriteLine("  topology get <topologyName>");
            Console.WriteLine("  topology set <toplogyFilePath>");
            Console.WriteLine("  topology delete <topologyName>");
            Console.WriteLine("  instance list");
            Console.WriteLine("  instance get <intanceName>");
            Console.WriteLine("  instance set <intanceName> <topologyName> -p <paramName=paramValue1");
            Console.WriteLine("  instance delete <intanceName>");
            Console.WriteLine("  instance activate <intanceName>");
            Console.WriteLine("  instance deactivate <intanceName>");

            Console.WriteLine();
            Console.WriteLine("For all of the topology and instance commands, using option -d <deviceId> and/or -m <moduleId> to override the device and module Id specified in the default connection.");
            Console.WriteLine();
                

            Console.WriteLine("Use the -h, -? option on any command for more details");

            Console.WriteLine();

        }

    }
}
