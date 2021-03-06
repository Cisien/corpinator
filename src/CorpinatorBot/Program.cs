﻿using CorpinatorBot.ConfigModels;
using CorpinatorBot.Discord;
using CorpinatorBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace CorpinatorBot
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
            .ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables("BOTHOSTING:");
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.AddUserSecrets("CorpinatorBot");
                }

                var secrets = new BotSecretsConfig();
                IConfigurationRoot builtConfig;
                if (context.HostingEnvironment.IsProduction())
                {
                    builtConfig = config.Build();
                    builtConfig.Bind(nameof(BotSecretsConfig), secrets);
                    config.AddAzureKeyVault(secrets.AkvVault, secrets.AkvClientId, secrets.AkvSecret);
                }

                // build and bind a second time to populate secrets found in AKV
                builtConfig = config.Build();
                builtConfig.Bind(nameof(BotSecretsConfig), secrets);

                context.Properties.Add("botSecrets", secrets);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.AddConsole();
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.AddDebug();
                }
            })
            .ConfigureServices((context, services) =>
            {
                var secrets = context.Properties["botSecrets"] as BotSecretsConfig;
                var socketConfig = new DiscordSocketConfig();
                context.Configuration.Bind(socketConfig);
                services.AddSingleton(socketConfig);

                services.AddSingleton(secrets);
                RegisterDiscordClient(services, socketConfig);
                services.AddSingleton<IVerificationStorageService, TableStorageVerificationStorageService>();
                services.AddSingleton<IGuildConfigService, TableStorageGuildConfigService>();
                services.AddTransient<IVerificationService, AzureVerificationService>();

                services.AddHostedService<DiscordBot>();
                services.AddHostedService<OngoingValidator>();
            })
            .UseConsoleLifetime();

            await hostBuilder.RunConsoleAsync();

        }

        private static void RegisterDiscordClient(IServiceCollection services, DiscordSocketConfig socketConfig)
        {
            var client = new DiscordSocketClient(socketConfig);

            services.AddSingleton<IDiscordClient>(client);
            services.AddSingleton(client);
        }
    }
}
