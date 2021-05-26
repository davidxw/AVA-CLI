using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.IO;
using System.CommandLine.IO;

namespace ava
{
    class Program
    {
        static List<string> CONNECTIONS_REQUIRED = new List<string> { "topology", "instance" };

        static IAvaCommandHandler _avaCommandHandler;

        static async Task Main(string[] args) => await BuildCommandLine()
            .UseHost(_ => new HostBuilder(),
                host =>
                {
                    host.ConfigureServices(services =>
                    {
                        // TBC
                    });
                })
            .UseMiddleware(async (context, next) =>
            {
                if (context.ParseResult.Tokens.Count >= 2 && CONNECTIONS_REQUIRED.Contains(context.ParseResult.Tokens[0].Value))
                {
                    if (context.ParseResult.HasOption("-c") && !string.IsNullOrEmpty((string)context.ParseResult.ValueForOption("-c")))
                    {
                        _avaCommandHandler.setConnectionString((string)context.ParseResult.ValueForOption("-c"));
                    }

                    if (context.ParseResult.HasOption("-d") && !string.IsNullOrEmpty((string)context.ParseResult.ValueForOption("-d")))
                    {
                        _avaCommandHandler.setDeviceId((string)context.ParseResult.ValueForOption("-d"));
                    }

                    if (context.ParseResult.HasOption("-m") && !string.IsNullOrEmpty((string)context.ParseResult.ValueForOption("-m")))
                    {
                        _avaCommandHandler.setModuleId((string)context.ParseResult.ValueForOption("-m"));
                    }
                }
                
                 await next(context);

            })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);

        private static CommandLineBuilder BuildCommandLine()
        {
            // required

            _avaCommandHandler = new AvaCommandHandler(new SystemConsole(), new FileConnectionHandler());

            var rootCommand = new RootCommand();

            var globalOptionConnectionString = new Option<string>("--connectionString", "Override the IoT Hub connection string in connection settings", ArgumentArity.ExactlyOne);
            globalOptionConnectionString.AddAlias("-c");

            rootCommand.AddGlobalOption(globalOptionConnectionString);

            var globalOptionDeviceId = new Option<string>("--deviceId", "Override the device Id in connection settings", ArgumentArity.ExactlyOne);
            globalOptionDeviceId.AddAlias("-d");

            rootCommand.AddGlobalOption(globalOptionDeviceId);

            var globalOptionModuleId = new Option<string>("--moduleId", "Override the module Id in connection settings", ArgumentArity.ExactlyOne);
            globalOptionModuleId.AddAlias("-m");

            rootCommand.AddGlobalOption(globalOptionModuleId);

            rootCommand.Handler = CommandHandler.Create(_avaCommandHandler.rootCommandHandler);

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

            connectionSetCommand.Handler = CommandHandler.Create<string, string, string>(_avaCommandHandler.connectionSetCommandHandler);

            connectCommand.Add(connectionSetCommand);

            // connection clear
            var connectionClearCommand = new Command("clear");
            connectionClearCommand.Handler = CommandHandler.Create(_avaCommandHandler.connectionClearCommandHandler);

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

            topologyListCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.topologyListCommandHandler);

            topologyCommand.Add(topologyListCommand);

            // ## topology get 

            var topologyGetCommand = new Command("get");

            var tga1 = new Argument<string>("topologyName", "The name of the topology to get");
            topologyGetCommand.AddArgument(tga1);

            topologyGetCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.topologyGetCommandHandler);

            topologyCommand.Add(topologyGetCommand);

            // ## topology set 

            var topologySetCommand = new Command("set");

            var tsa1 = new Argument<FileInfo>("topologyFile", "A file containing full topology specification");
            topologySetCommand.AddArgument(tsa1);

            topologySetCommand.Handler = CommandHandler.Create<FileInfo>(_avaCommandHandler.topologySetCommandHandler);

            topologyCommand.Add(topologySetCommand);

            // ## topology delete 

            var topologyDeleteCommand = new Command("delete");

            var tda1 = new Argument<string>("topologyName", "The name of the topology to delete");
            topologyDeleteCommand.AddArgument(tda1);

            topologyDeleteCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.topologyDeleteCommandHandler);

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

            instanceListCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.instanceListCommandHandler);

            instanceCommand.Add(instanceListCommand);

            // ## instance get 

            var instanceGetCommand = new Command("get");

            var iga1 = new Argument<string>("instanceName", "The name of the instance to get");
            instanceGetCommand.AddArgument(iga1);

            instanceGetCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.instanceGetCommandHandler);

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

            instanceSetCommand.Handler = CommandHandler.Create<string, string, string[]>(_avaCommandHandler.instanceSetCommandHandler);

            instanceCommand.Add(instanceSetCommand);

            // ## instance activate 

            var instanceActivateCommand = new Command("activate");

            var iaa1 = new Argument<string>("instanceName", "The name of the instance to activate");
            instanceActivateCommand.AddArgument(iaa1);

            instanceActivateCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.instanceActivateCommandHandler);

            instanceCommand.Add(instanceActivateCommand);

            // ## instance deactivate 

            var instanceDeactivateCommand = new Command("deactivate");

            var ida1 = new Argument<string>("instanceName", "The name of the instance to deactivate");
            instanceDeactivateCommand.AddArgument(ida1);

            instanceDeactivateCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.instanceDeactivateCommandHandler);

            instanceCommand.Add(instanceDeactivateCommand);

            // ## instance delete 

            var instanceDeleteCommand = new Command("delete");

            var idea1 = new Argument<string>("instanceName", "The name of the instance to delete");
            instanceDeleteCommand.AddArgument(idea1);

            instanceDeleteCommand.Handler = CommandHandler.Create<string>(_avaCommandHandler.instanceDeleteCommandHandler);

            instanceCommand.Add(instanceDeleteCommand);

            return new CommandLineBuilder(rootCommand);
        }
    }
}
