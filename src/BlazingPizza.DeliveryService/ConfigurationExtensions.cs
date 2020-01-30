using System;
using Microsoft.Extensions.Configuration;

namespace BlazingPizza
{
    public static class ConfigurationExtensions
    {
        public static string GetServiceHostname(this IConfiguration configuration, string name, string @default = default)
        {
            var value = configuration[$"Service:{name}"];
            if (!string.IsNullOrEmpty(value) && @default is null)
            {
                return value;
            }
            else if (@default != null)
            {
                return @default;
            }
            else
            {
                throw new InvalidOperationException($"Could not find a configuration value for Service:{name}.");
            }
        }
    }
}