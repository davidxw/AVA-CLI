using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ava
{
    public class FileConnectionHandler : IConnectionHandler
    {
        const string CONNECTION_SETTINGS_FILENAME = "connection.json";

        public string IoTHubConnectionString { get; set; }

        public string DeviceId { get; set; }

        public string ModuleId { get; set; }

        public ConnectionSettings ConnectionSettings => new ConnectionSettings { IoTHubConnectionString = IoTHubConnectionString, DeviceId = DeviceId, ModuleId = ModuleId };

        public bool IsValid => !string.IsNullOrEmpty(IoTHubConnectionString) && !string.IsNullOrEmpty(DeviceId) && !string.IsNullOrEmpty(ModuleId);

        public FileConnectionHandler()
        {
            LoadConnectionSettings();
        }

        public void Clear()
        {
            File.Delete(GetConnectionSettingsFilePath());
        }

        public void Persist()
        {
            var connectionSettings = new ConnectionSettings { IoTHubConnectionString = this.IoTHubConnectionString, DeviceId = this.DeviceId, ModuleId = this.ModuleId };

            File.WriteAllText(GetConnectionSettingsFilePath(), JsonConvert.SerializeObject(connectionSettings));
        }

        private void LoadConnectionSettings()
        {
            ConnectionSettings connectionSettings = null;

            try
            {
                var connectSettingsFileContent = File.ReadAllText(GetConnectionSettingsFilePath());

                if (string.IsNullOrEmpty(connectSettingsFileContent))
                    return;

                connectionSettings = JsonConvert.DeserializeObject<ConnectionSettings>(connectSettingsFileContent);
            }
            catch (Exception)
            {
                return;
            }

            if (connectionSettings != null)
            {
                IoTHubConnectionString = connectionSettings.IoTHubConnectionString;
                DeviceId = connectionSettings.DeviceId;
                ModuleId = connectionSettings.ModuleId;
            }
        }

        private string GetConnectionSettingsFilePath()
        {
            var connectionSettingFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ava-cli");

            Directory.CreateDirectory(connectionSettingFileDir);

            return Path.Combine(connectionSettingFileDir, CONNECTION_SETTINGS_FILENAME);
        }
    }
}
