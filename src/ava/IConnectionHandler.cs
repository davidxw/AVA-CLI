using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ava
{
    public interface IConnectionHandler
    {
        string IoTHubConnectionString { get; set; }

        string DeviceId { get; set; }

        string ModuleId { get; set; }

        ConnectionSettings ConnectionSettings { get; }

        bool IsValid { get; }

        void Persist();

        void Clear();
    }
}
