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

        Task topologyListCommandHandler(string query);

        Task topologyGetCommandHandler(string topologyName);

        Task topologySetCommandHandler(FileInfo topologyFile);

        Task topologyDeleteCommandHandler(string topologyName);

        Task instanceListCommandHandler(string query);

        Task instanceGetCommandHandler(string instanceName);

        Task instanceSetCommandHandler(string instanceName, string topologyName, string[] paramater);

        Task instanceActivateCommandHandler(string instanceName);

        Task instanceDeactivateCommandHandler(string instanceName);

        Task instanceDeleteCommandHandler(string instanceName);




    }
}
