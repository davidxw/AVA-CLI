using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ava
{
    public class ConnectionSettings
    {
        public string IoTHubConnectionString { get; set; }

        public string DeviceId { get; set; }

        public string ModuleId { get; set; }
    }
}
