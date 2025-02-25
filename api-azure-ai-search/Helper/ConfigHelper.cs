using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
namespace api_azure_ai_search.Helper
{

    /// <summary>
    /// This approach is much more elegant than using the IConfiguration object for he following reasons.
    /// 1. It's simpler - you don't need any dependency injection or complex configuration objects
    /// 2. Your ConfigHelper stays very lightweight and straightforward
    /// 3. You can access the variables from anywhere without needing to pass around configuration objects
    /// 4. It maintains consistency - whether the values come from actual environment variables or appsettings.Local.json, your code accesses them the same way
    /// 
    /// Important Notes:
    /// In Program.cs you need to make sure the following commands are executed so the appsettiongs.Local.json file is loaded
    /// builder.Configuration
    ///    .SetBasePath(Directory.GetCurrentDirectory())
    ///    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    ///    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    ///    
    /// How to access the variables from the appsettings.Local.json file?
    ///   var openAIEndpoint = ConfigHelper.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    /// </summary>
    public static class ConfigHelper
    {
        public static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process) ?? "";
        }
    }
}
