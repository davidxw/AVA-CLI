using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ava
{
    public interface IAvaCommandHandler
    {
        void rootCommandHandler();

        void connectionSetCommandHandler(string connectionString, string deviceId, string moduleId);

        void connectionClearCommandHandler();

        void setConnectionString(string connectionString);

        void setDeviceId(string deviceId);

        void setModuleId(string moduleId);

        Task topologyListCommandHandler();

        Task topologyGetCommandHandler(string topologyName);

        Task topologySetCommandHandler(FileInfo topologyFile);

        Task topologyDeleteCommandHandler(string topologyName);

        Task pipelineListCommandHandler();

        Task pipelineGetCommandHandler(string pipelineName);

        Task pipelineSetCommandHandler(string pipelineName, string topologyName, string[] paramater);

        Task pipelineActivateCommandHandler(string pipelineName);

        Task pipelineDeactivateCommandHandler(string pipelineName);

        Task pipelineDeleteCommandHandler(string pipelineName);




    }
}
