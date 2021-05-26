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

        private static Option CreateStringOptionWithAliases(string alias1, string alias2, string description, IArgumentArity arity)
        {
            var option = new Option<string>(alias1, description, arity);
            option.AddAlias(alias2);

            return option;
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            _avaCommandHandler = new AvaCommandHandler(new SystemConsole(), new FileConnectionHandler());

            var rootCommand = new RootCommand()
            {
                Handler = CommandHandler.Create(_avaCommandHandler.rootCommandHandler)
            };

            rootCommand.AddGlobalOption(CreateStringOptionWithAliases("--connectionString", "-c", "Override the IoT Hub connection string in connection settings", ArgumentArity.ExactlyOne));
            rootCommand.AddGlobalOption(CreateStringOptionWithAliases("--deviceId", "-d", "Override the device Id in connection settings", ArgumentArity.ExactlyOne));
            rootCommand.AddGlobalOption(CreateStringOptionWithAliases("--moduleId", "-m", "Override the module Id in connection settings", ArgumentArity.ExactlyOne));

            // connection

            var connectCommand = new Command("connection");

            rootCommand.Add(connectCommand);

            // connection set

            var connectionSetCommand = new Command("set")
            { 
                Handler = CommandHandler.Create<string, string, string>(_avaCommandHandler.connectionSetCommandHandler) 
            };

            connectionSetCommand.AddArgument(new Argument<string>("connectionString", "A connection string for the IoTHub"));
            connectionSetCommand.AddArgument(new Argument<string>("deviceId", "The IoT Edge device Id"));
            connectionSetCommand.AddArgument(new Argument<string>("moduleId", "The AVA module Id"));;

            connectCommand.Add(connectionSetCommand);

            // connection clear
            var connectionClearCommand = new Command("clear")
            {
                Handler = CommandHandler.Create(_avaCommandHandler.connectionClearCommandHandler)
            };

            connectCommand.Add(connectionClearCommand);

            // ######### topology

            var topologyCommand = new Command("topology");

            rootCommand.Add(topologyCommand);

            // ## topology list 

            var topologyListCommand = new Command("list")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.topologyListCommandHandler)
            };

            //topologyListCommand.AddOption(CreateStringOptionWithAliases("--query", "-q", "Optionally apply an ODATA query to the results", ArgumentArity.ZeroOrOne));

            topologyCommand.Add(topologyListCommand);

            // ## topology get 

            var topologyGetCommand = new Command("get")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.topologyGetCommandHandler)
            };

            topologyGetCommand.AddArgument(new Argument<string>("topologyName", "The name of the topology to get"));

            topologyCommand.Add(topologyGetCommand);

            // ## topology set 

            var topologySetCommand = new Command("set")
            {
                Handler = CommandHandler.Create<FileInfo>(_avaCommandHandler.topologySetCommandHandler)
            };

            topologySetCommand.AddArgument(new Argument<FileInfo>("topologyFile", "A file containing full topology specification"));

            topologyCommand.Add(topologySetCommand);

            // ## topology delete 

            var topologyDeleteCommand = new Command("delete")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.topologyDeleteCommandHandler)
            };

            topologyDeleteCommand.AddArgument(new Argument<string>("topologyName", "The name of the topology to delete"));

            topologyCommand.Add(topologyDeleteCommand);

            // ######### instance 

            var instanceCommand = new Command("instance");

            rootCommand.Add(instanceCommand);

            // ## instance list 

            var instanceListCommand = new Command("list")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineListCommandHandler)
            };

            //instanceListCommand.AddOption(CreateStringOptionWithAliases("--query", "-q", "Optionally apply an ODATA query to the results", ArgumentArity.ZeroOrOne)););

            instanceCommand.Add(instanceListCommand);

            // ## instance get 

            var instanceGetCommand = new Command("get")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineGetCommandHandler)
            };

            instanceGetCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to get"));

            instanceCommand.Add(instanceGetCommand);

            // ## instance set 

            var instanceSetCommand = new Command("set")
            {
                Handler = CommandHandler.Create<string, string, string[]>(_avaCommandHandler.pipelineSetCommandHandler)
            };

            instanceSetCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to set"));
            instanceSetCommand.AddArgument(new Argument<string>("topologyName", "The name of the topology to use for the pipeline"));

            instanceSetCommand.AddOption(CreateStringOptionWithAliases("--paramater", "-p", "A paramater to set on the instaince in the format 'paramName=paramValue'", ArgumentArity.ZeroOrMore));

            instanceCommand.Add(instanceSetCommand);

            // ## instance activate 

            var instanceActivateCommand = new Command("activate")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineActivateCommandHandler)
            };

            instanceActivateCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to activate"));

            instanceCommand.Add(instanceActivateCommand);

            // ## instance deactivate 

            var instanceDeactivateCommand = new Command("deactivate")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineDeactivateCommandHandler)
            };

            instanceDeactivateCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to deactivate"));

            instanceCommand.Add(instanceDeactivateCommand);

            // ## instance delete 

            var instanceDeleteCommand = new Command("delete")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineDeleteCommandHandler)
            };

            instanceDeleteCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to delete"));

            instanceCommand.Add(instanceDeleteCommand);

            return new CommandLineBuilder(rootCommand);
        }
    }
}
