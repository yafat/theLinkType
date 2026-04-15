using Microsoft.Extensions.Configuration;
using theLinkType.Models;
using theLinkType.Models;

namespace theLinkType.Services;

public sealed class AppConfigService
{
    public AppConfig Load()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appConfig = new AppConfig();
        config.Bind(appConfig);
        return appConfig;
    }
}