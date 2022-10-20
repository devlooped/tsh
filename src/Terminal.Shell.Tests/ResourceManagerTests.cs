using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Shell;

public class ResourceManagerTests
{
    [Fact]
    public void CanLoadResource()
    {
        using (var provider = CompositionSetup.CreateDefaultProvider())
        {
            var manager = provider.GetExportedValue<IResourceManager>();

            Assert.Equal("Exit", manager.GetString("File.Exit:Default"));
        }

        Thread.CurrentThread.CurrentUICulture = new CultureInfo("es");

        using (var provider = CompositionSetup.CreateDefaultProvider())
        {
            var manager = provider.GetExportedValue<IResourceManager>();

            Assert.Equal("Salir", manager.GetString("File.Exit:Default"));
        }

        Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-AR");

        using (var provider = CompositionSetup.CreateDefaultProvider())
        {
            var manager = provider.GetExportedValue<IResourceManager>();

            Assert.Equal("Chau", manager.GetString("File.Exit:Default"));
        }
    }
}
