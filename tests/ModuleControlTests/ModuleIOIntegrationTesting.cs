using ModuleControl.Communication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModuleControlTests
{
    // note, this class is just meant for testing the module, gathering data, etc. On the fly stuff so I don't have to come up with a complex main and build outside ide
    public class ModuleIOIntegrationTesting
    {
        private readonly string _dumpDataPath;
        private string _dumpDataFolderName = "DataDump";

        string _dataComName = "COM9";
        string _cLIComName = "COM8";

        int _numFramesToSave = 50;
        int _framesSaved = 0;

        public ModuleIOIntegrationTesting()
        {
            _dumpDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dumpDataFolderName);
            StartModule();
        }

        [Fact]
        public void CollectData()
        {
            ModuleIO.Instance.OnFrameProcessed += OnFrameProcessed_Save;

            while (_framesSaved < _numFramesToSave)
            {
                Thread.Sleep(10);
            }

            ModuleIO.Instance.Stop();
            ModuleIO.Instance.OnFrameProcessed -= OnFrameProcessed_Save;
        }

        private void OnFrameProcessed_Save(object? sender, FrameEventArgs e)
        {
            _framesSaved++;
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(e.FrameEvent, settings);

            File.WriteAllText(_dumpDataFolderName, json);
        }

        private void StartModule()
        {
            ModuleIO.Instance.InitializePorts(_dataComName, _cLIComName);
            ModuleIO.Instance.TrySendConfig();
            ModuleIO.Instance.StartDataPolling();
        }


    }
}
