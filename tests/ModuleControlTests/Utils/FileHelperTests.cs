using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModuleControl.Utils;

namespace ModuleControlTests.Utils
{
    public class FileHelperTests
    {
        [Fact]
        public void FindAndReadConfigFileShouldHaveOurFile()
        {
            var file = FileHelper.FindAndReadConfigFile();
            file.Should().NotBeEmpty().And.Contain("sensorStop");
        }
    }
}
