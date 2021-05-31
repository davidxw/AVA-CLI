using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace ava
{
    class Program
    {
        static List<string> CONNECTIONS_REQUIRED = new List<string> { "topology", "pipeline" };

        static IAvaCommandHandler _avaCommandHandler;

        static async Task Main(string[] args) => await BuildCommandLine()
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
            _avaCommandHandler = new AvaCommandHandler(new SystemConsole(), new FileConnectionHandler());

            var rootCommand = new RootCommand()
            {
                Handler = CommandHandler.Create(_avaCommandHandler.rootCommandHandler)
            };

            rootCommand.AddGlobalOption(CreateStringOptionWithAliases("--connectionString", "-c", "Override the IoT Hub connection string in connection settings", ArgumentArity.ExactlyOne));
            rootCommand.AddGlobalOption(CreateStringOptionWithAliases("--deviceId", "-d", "Override the device Id in connection settings", ArgumentArity.ExactlyOne));
            rootCommand.AddGlobalOption(CreateStringOptionWithAliases("--moduleId", "-m", "Override the module Id in connection settings", ArgumentArity.ExactlyOne));

            // ######### connection

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

            // ######### pipeline 

            var pipelineCommand = new Command("pipeline");

            rootCommand.Add(pipelineCommand);

            // ## pipeline list 

            var pipelineListCommand = new Command("list")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineListCommandHandler)
            };

            //pipelineListCommand.AddOption(CreateStringOptionWithAliases("--query", "-q", "Optionally apply an ODATA query to the results", ArgumentArity.ZeroOrOne)););

            pipelineCommand.Add(pipelineListCommand);

            // ## pipeline get 

            var pipelineGetCommand = new Command("get")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineGetCommandHandler)
            };

            pipelineGetCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to get"));

            pipelineCommand.Add(pipelineGetCommand);

            // ## pipeline set 

            var pipelineSetCommand = new Command("set")
            {
                Handler = CommandHandler.Create<string, string, string[]>(_avaCommandHandler.pipelineSetCommandHandler)
            };

            pipelineSetCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to set"));
            pipelineSetCommand.AddArgument(new Argument<string>("topologyName", "The name of the topology to use for the pipeline"));

            pipelineSetCommand.AddOption(CreateStringOptionWithAliases("--paramater", "-p", "A paramater to set on the pipeline in the format 'paramName=paramValue'", ArgumentArity.ZeroOrMore));

            pipelineCommand.Add(pipelineSetCommand);

            // ## pipeline activate 

            var pipelineActivateCommand = new Command("activate")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineActivateCommandHandler)
            };

            pipelineActivateCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to activate"));

            pipelineCommand.Add(pipelineActivateCommand);

            // ## pipeline deactivate 

            var pipelineDeactivateCommand = new Command("deactivate")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineDeactivateCommandHandler)
            };

            pipelineDeactivateCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to deactivate"));

            pipelineCommand.Add(pipelineDeactivateCommand);

            // ## pipeline delete 

            var pipelineDeleteCommand = new Command("delete")
            {
                Handler = CommandHandler.Create<string>(_avaCommandHandler.pipelineDeleteCommandHandler)
            };

            pipelineDeleteCommand.AddArgument(new Argument<string>("pipelineName", "The name of the pipeline to delete"));

            pipelineCommand.Add(pipelineDeleteCommand);

            return new CommandLineBuilder(rootCommand);
        }

        private static Option CreateStringOptionWithAliases(string alias1, string alias2, string description, IArgumentArity arity)
        {
            var option = new Option<string>(alias1, description, arity);
            option.AddAlias(alias2);

            return option;
        }
    }
}
