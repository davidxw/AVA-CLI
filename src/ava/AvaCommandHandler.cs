using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ava
{
    public class AvaCommandHandler : IAvaCommandHandler
    {
        private IConsole _console;
        private IConnectionHandler _connection;

        private ITerminal _terminal;

        const string ROOT_COMMAND_NAME = "ava";

        const string CONNECTION_SETTINGS_FILENAME = "connection.json";

        const string GRAPH_TOPOLOGY_LABEL = "Graph topology";
        const string GRAPH_INSTANCE_LABEL = "Graph instance";

        public AvaCommandHandler(IConsole console, IConnectionHandler connection)
        {
            _console = console;
            _terminal = new SystemConsoleTerminal(_console);
            _connection = connection;
        }

        public void setConnectionString(string connectionString)
        {
            _connection.IoTHubConnectionString = connectionString;
        }

        public void setDeviceId(string deviceId)
        {
            _connection.DeviceId = deviceId;
        }

        public void setModuleId(string moduleId)
        {
            _connection.ModuleId = moduleId;
        }

        public void connectionClearCommandHandler()
        {
            _connection.Clear();

            _terminal.Out.WriteLine("Connection details cleared");
        }

        public void connectionSetCommandHandler(string connectionString, string deviceId, string moduleId)
        {
            _connection.IoTHubConnectionString = connectionString;
            _connection.ModuleId = moduleId;
            _connection.DeviceId = deviceId;

            _connection.Persist();

            _terminal.Out.WriteLine("Connection details saved");
        }

        public async Task topologyListCommandHandler(string query)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(
                _connection.ConnectionSettings, 
                "GraphTopologyList");

            var output = await command.ExecuteList(query);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_TOPOLOGY_LABEL);
            }
            else
            {
                var f = "{0,-30} {1,-30}";

                _terminal.Out.WriteLine(String.Format(f, "Name", "Date Created (UTC)").ToString());
                try
                {
                    var outputRows = new List<string>();

                    foreach (var t in output.ResponseBody.value)
                    {
                        outputRows.Add(String.Format(f, t.name, t.systemData.createdAt));
                    }

                    _terminal.Out.WriteLine(string.Join(Environment.NewLine, outputRows));
                }
                catch
                {
                    _terminal.ForegroundColor = ConsoleColor.Red;
                    _terminal.Out.WriteLine("Failed to parse response from AVA. The raw response received was:");
                    _terminal.ResetColor();
                    _terminal.Out.WriteLine(output.ResponseBodyString);
                }
            }
        }

        public async Task topologyGetCommandHandler(string topologyName)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphTopologyGet");
            var output = await command.Execute(topologyName);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_TOPOLOGY_LABEL, topologyName);
            }
            else
            {
                _terminal.Out.WriteLine(output.ResponseBodyString);
            }
        }

        public async Task topologySetCommandHandler(FileInfo topologyFile)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphTopologySet");
            var output = await command.Execute(topologyFile);

            WriteResult(output, GRAPH_TOPOLOGY_LABEL);
        }

        public async Task topologyDeleteCommandHandler(string topologyName)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphTopologyDelete");
            var output = await command.Execute(topologyName);

            WriteResult(output, GRAPH_TOPOLOGY_LABEL, topologyName, "deleted", null, "is being referenced by more than one graph instance and cannot be deleted");
        }

        public async Task instanceListCommandHandler(string query)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphInstanceList");
            var output = await command.ExecuteList(query);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_INSTANCE_LABEL);
            }
            else
            {
                var f = "{0,-30} {1,-30} {2, -10} {3, -30}";

                _terminal.Out.WriteLine(String.Format(f, "Name", "Date Created (UTC)", "State", "Topology"));

                try
                {
                    var outputRows = new List<string>();

                    foreach (var t in output.ResponseBody.value)
                    {
                        outputRows.Add(String.Format(f, t.name, t.systemData.createdAt, t.properties.state, t.properties.topologyName));
                    }

                    _terminal.Out.WriteLine(string.Join(Environment.NewLine, outputRows));
                }
                catch
                {
                    _terminal.ForegroundColor = ConsoleColor.Red;
                    _terminal.Out.WriteLine("Failed to parse response from AVA. The raw response received was:");
                    _terminal.ResetColor();
                    _terminal.Out.WriteLine(output.ResponseBodyString);
                }
            }
        }

        public async Task instanceGetCommandHandler(string instanceName)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphInstanceGet");
            var output = await command.Execute(instanceName);

            if (!output.IsSuccess)
            {
                WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName);
            }
            else
            {
                _terminal.Out.WriteLine(output.ResponseBodyString);
            }
        }

        public async Task instanceSetCommandHandler(string instanceName, string topologyName, string[] paramater)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphInstanceSet");
            var output = await command.Execute(instanceName, topologyName, paramater);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "updated", "created", " already exists.");

            if (output.IsSuccess)
            {
                _terminal.Out.WriteLine(output.ResponseBodyString);
            }
        }

        public async Task instanceActivateCommandHandler(string instanceName)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphInstanceActivate");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "activated");
        }

        public async Task instanceDeactivateCommandHandler(string instanceName)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphInstanceDeactivate");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "deactivated");
        }

        public async Task instanceDeleteCommandHandler(string instanceName)
        {
            if (!ValidateConnectionDetails())
                return;

            var command = new AvaDirectMethod(_connection.ConnectionSettings, "GraphInstanceDelete");
            var output = await command.Execute(instanceName);

            WriteResult(output, GRAPH_INSTANCE_LABEL, instanceName, "deleted", null, $"is in an active state and cannot be deleted. Run '{ROOT_COMMAND_NAME} instance deactivate {instanceName}' to decativate.");
        }

        private bool ValidateConnectionDetails()
        {
            if (_connection.IsValid)
                return true;

            _terminal.ForegroundColor = ConsoleColor.Red;
            _terminal.Out.WriteLine($"No connection details provided. Run '{ROOT_COMMAND_NAME} connection set', or use the --connectionString, --deviceId and --moduleId parameters");
            _terminal.ResetColor();

            return false;
        }

        private void WriteResult(DirectMethodResponse output, string entity, string instanceName = null, string action200 = "updated", string action201 = "created", string message409 = null)
        {
            instanceName = string.IsNullOrEmpty(instanceName) ? null : $" {instanceName}";

            switch (output.ResponseCode)
            {
                case 0:
                    _terminal.Out.WriteLine($"Operation failed - {output.ResponseBodyString}");
                    break;
                case 200:
                    _terminal.Out.WriteLine($"{entity}{instanceName} {action200}.");
                    break;
                case 201:
                    _terminal.Out.WriteLine($"{entity}{instanceName} {action201}.");
                    break;
                case 204:
                    _terminal.ForegroundColor = ConsoleColor.Yellow;
                    _terminal.Out.WriteLine($"{entity}{instanceName} does not exist");
                    _terminal.ResetColor();
                    break;
                case 404:
                    _terminal.ForegroundColor = ConsoleColor.Yellow;
                    _terminal.Out.WriteLine($"{entity}{instanceName} does not exist");
                    _terminal.ResetColor();
                    break;
                case 409:
                    _terminal.ForegroundColor = ConsoleColor.Yellow;
                    _terminal.Out.WriteLine($"{entity}{instanceName} {message409}");
                    _terminal.ResetColor();
                    break;
                default:
                    _terminal.ForegroundColor = ConsoleColor.Red;
                    _terminal.Out.WriteLine($"Operation failed - response code {output.ResponseCode}");
                    _terminal.ResetColor();
                    _terminal.Out.WriteLine(output.ResponseBodyString);
                    break;
            }
        }

        public void rootCommandHandler()
        {
            _terminal.Out.WriteLine();
            _terminal.ForegroundColor = ConsoleColor.Blue;

            _terminal.Out.WriteLine("AVA CLI - a simple CLI for Azure Video Analytics");
            _terminal.ResetColor();
            _terminal.Out.WriteLine();

            _terminal.Out.WriteLine("Current connection settings:");

            _terminal.Out.WriteLine($"  IoT Hub:   {_connection.IoTHubConnectionString}");
            _terminal.Out.WriteLine($"  Device Id: {_connection.DeviceId}");
            _terminal.Out.WriteLine($"  Module Id: {_connection.ModuleId}");

            _terminal.Out.WriteLine();

            _terminal.Out.WriteLine("Commands:");
            _terminal.Out.WriteLine("  connection set <connectionString> <deviceId> <moduleId>");
            _terminal.Out.WriteLine("  connection clear");
            _terminal.Out.WriteLine("  topology list");
            _terminal.Out.WriteLine("  topology get <topologyName>");
            _terminal.Out.WriteLine("  topology set <toplogyFilePath>");
            _terminal.Out.WriteLine("  topology delete <topologyName>");
            _terminal.Out.WriteLine("  instance list");
            _terminal.Out.WriteLine("  instance get <intanceName>");
            _terminal.Out.WriteLine("  instance set <intanceName> <topologyName> -p <paramName=paramValue1");
            _terminal.Out.WriteLine("  instance delete <intanceName>");
            _terminal.Out.WriteLine("  instance activate <intanceName>");
            _terminal.Out.WriteLine("  instance deactivate <intanceName>");

            _terminal.Out.WriteLine();
            _terminal.Out.WriteLine("For all of the topology and instance commands, using option -c <connectionString>, -d <deviceId> and/or -m <moduleId> to override the device and module Id specified in the default connection.");
            _terminal.Out.WriteLine();


            _terminal.Out.WriteLine("Use the -h, -? option on any command for more details");

            _terminal.Out.WriteLine();

        }
    }
}
