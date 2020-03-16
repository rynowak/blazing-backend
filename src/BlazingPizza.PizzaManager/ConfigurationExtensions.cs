using System;
using Microsoft.Extensions.Configuration;

namespace BlazingPizza
{
    public static class ConfigurationExtensions
    {
        public static string GetServiceHostname(this IConfiguration configuration, string name, string @default = default)
        {
            var connectionString = configuration[$"service:{name}:connectionstring"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            var host = configuration[$"service:{name}:host"];
            var port = configuration[$"service:{name}:port"];
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
            {
                return $"{host}:{port}";
            }

            if (@default != null)
            {
                return @default;
            }
            else
            {
                throw new InvalidOperationException($"Could not find a configuration value for s ervice:{name}.");
            }
        }

        public static Uri GetServiceUri(this IConfiguration configuration, string name, string @default = default)
        {
            var connectionString = configuration[$"service:{name}:connectionstring"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                return new Uri(connectionString);
            }

            var host = configuration[$"service:{name}:host"];
            var port = configuration[$"service:{name}:port"];
            var protocol = configuration[$"service:{name}:protocol"] ?? "http";
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
            {
                return new Uri($"{protocol}://{host}:{port}");
            }

            if (@default != null)
            {
                return new Uri(@default);
            }
            else
            {
                throw new InvalidOperationException($"Could not find a configuration value for Service:{name}.");
            }
        }
    }
}