using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Configurations;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using Color = Discord.Color;

namespace MonopolyDiscordBot
{
    public class Program
    {
        public static Dictionary<ulong, PlayerToken[]> GuildTokens = new();
        internal Secret Secret { get; set; }
        private IServiceProvider services;
        private IServiceCollection serviceCollection;
        public static IServiceProvider Services { get; private set; }        
        public static Task Main(string[] args) => new Program().MainAsync();        
        public IServiceProvider CreateServices()
        {
            var config = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose
            };
            var servConfig = new InteractionServiceConfig()
            {
                LogLevel = LogSeverity.Debug, 
                WildCardExpression = "*"
            };
            var collection = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton(servConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<DataAccessLayer>(x =>
                {
                    var layer = new DataAccessLayer(x.GetRequiredService<DiscordSocketClient>(), x)
                    {
                        MortgageTitleDeedsImagesFolder = Secret.TitleDeedDirPath + "\\MortgagedSide",
                        TitleDeedsImagesFolder = Secret.TitleDeedDirPath

                    };

                    return layer;
                    })
                .AddSingleton<BaseMonopoly.Configurations.Configuration>(DiscordGame.GetEmptyStandardConfiguration);

            this.serviceCollection = collection;
            return collection.BuildServiceProvider();
        }
        private async Task MainAsync()
        {
            var secret = JsonConvert.DeserializeObject<Secret>("Config.json");
            var token = secret.BotToken;            
            this.services = this.CreateServices();
            Program.Services = services;
            var client = services.GetRequiredService<DiscordSocketClient>();
            await client.LoginAsync(Discord.TokenType.Bot, token);
            await client.StartAsync();
            client.Ready += ClientReady;                 
            
            client.Log += (x) => { Console.WriteLine(x.ToString()); return Task.CompletedTask; };              
            client.InteractionCreated += SocketInteractionCreated;
            
            Console.WriteLine(client.LoginState);

            await Task.Delay(-1);
        }        
        
        private async Task SocketInteractionCreated(SocketInteraction socketInteraction)
        {
            InteractionService interactionService = services.GetRequiredService<InteractionService>();            
            DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
            interactionService.SlashCommandExecuted += SlashCommandCheckSuccess;
            var ctx = new SocketInteractionContext(client, socketInteraction);
            try
            {                

                var taskRecieved = await interactionService.ExecuteCommandAsync(ctx, services);
                
            }catch (Exception ex)
            {
                await socketInteraction.RespondAsync(ex.Message, ephemeral: true);
            }
            
        }

        private async Task SlashCommandCheckSuccess(SlashCommandInfo slashCommandInfo, IInteractionContext interactionContext, Discord.Interactions.IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await interactionContext.Interaction.RespondAsync($"Unmet Precondition: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.UnknownCommand:
                        await interactionContext.Interaction.RespondAsync("Unknown command");
                        break;
                    case InteractionCommandError.BadArgs:
                        await interactionContext.Interaction.RespondAsync("Invalid number or arguments");
                        break;
                    case InteractionCommandError.Exception:
                        await interactionContext.Interaction.RespondAsync($"Command failed: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.Unsuccessful:
                        await interactionContext.Interaction.RespondAsync("Command could not be executed");
                        break;
                    default:
                        break;
                }
            }
        }
        private async Task ClientReady()
        {
            await RegisterModules();
        }
        
        private async Task RegisterModules()
        {
            var service = services.GetRequiredService<InteractionService>();
            service.Log += (x) => { Console.WriteLine(x.ToString()); return Task.CompletedTask; };
            await service.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            await service.RegisterCommandsGloballyAsync();            
        }        
    }
    
}
