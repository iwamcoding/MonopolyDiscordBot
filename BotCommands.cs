using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    public class BotCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public DataAccessLayer DataAccess { get; set; }
        public DiscordSocketClient Client { get; set; }     
        
        private SelectMenuBuilder CreatePlayerMenu()
        {
            var preGamePlayers = DataAccess.GetPreGamePlayers(Context.Guild.Id);
            var allTokens = Enum.GetValues(typeof(PlayerToken)).Cast<PlayerToken>().ToList();
            if (preGamePlayers != null)
            {
                var tokens = preGamePlayers.Select(x => x.PlayerToken);
                foreach(var token in tokens)                
                {
                    allTokens.Remove(token);                   
                }
            }

            if (allTokens.Count == 0)
                return null;

            var playerMenu = new SelectMenuBuilder()
            {
                Placeholder = "Select your token",
                CustomId = "player-menu",
            };
            
            foreach (var token in allTokens)
            {
                playerMenu.AddOption(token.ToString(), token.ToString(), $"Select {token}");
            }

            return playerMenu;                        
        }
        [SlashCommand("add-players", "Adds players for a game")]
        public async Task AddPlayers()
        {
            var menu = CreatePlayerMenu() ?? throw new InvalidOperationException("Select menu could not be created.");
            var builder = new ComponentBuilder().WithSelectMenu(menu);
            await RespondAsync("Add Players", components: builder.Build());
        }
        [ComponentInteraction("player-menu")]
        public async Task RespondToPlayerMenu(string value)
        {            
            PlayerToken token = (PlayerToken)Enum.Parse(typeof(PlayerToken), value);
            DataAccess.AddPreGamePlayer(Context.Guild.Id, Context.User.Id, token);

            var allPreGamePlayers = DataAccess.GetPreGamePlayers(Context.Guild.Id);

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Player Tokens",
            };
            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var preGamePlayer in allPreGamePlayers)
            {
                var userName = Client.GetUser(preGamePlayer.UserId).Username;
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{preGamePlayer.PlayerToken}:",
                    Value = userName
                });
            }

            embedBuilder.WithFields(fieldBuilders);
            var menu = CreatePlayerMenu();
            if (menu == null)
                await (Context.Interaction as IComponentInteraction).UpdateAsync(x => { x.Embed = embedBuilder.Build(); x.Components = null; });
            else
            {
                var menuBuilder = new ComponentBuilder().WithSelectMenu(menu);
                await (Context.Interaction as IComponentInteraction).UpdateAsync(x => { x.Embed = embedBuilder.Build(); x.Components = menuBuilder.Build(); }) ;
            }
            
            
        }


        [SlashCommand("start-game", "Starts a standard game for tokens in guild")]
        public async Task StartGame()
        {
            DataAccess.AddGame(Context.Guild.Id);
            var game = DataAccess.GetLastGame(Context.Guild.Id);
            game.InitializePlayers();

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Players",
                Description = "Players' turn order"
            };
            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var player in game.GetDiscordPlayers())
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{player.Username} ({player.PlayerToken})",
                    Value = $"rolled {player.GetDiceSum()}"
                });
            }
            embedBuilder.WithFields(fieldBuilders);

            game.StartGame();
            await RespondAsync(embed: embedBuilder.Build());
        }


    }
}
