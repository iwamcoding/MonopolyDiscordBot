using BaseMonopoly;
using BaseMonopoly.Assets.BoardAssets;
using BaseMonopoly.Assets.BoardAssets.ActionCardAssets;
using BaseMonopoly.Assets.TransactionAssets;
using BaseMonopoly.Assets.TransactionAssets.TitleDeedAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Commands.StreetCommands;
using BaseMonopoly.Configurations;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Configuration = BaseMonopoly.Configurations.Configuration;

namespace MonopolyDiscordBot
{
    public class DataAccessLayer
    {
        public string TitleDeedsImagesFolder { get; set; }
        public string MortgageTitleDeedsImagesFolder { get; set; }
        private Dictionary<string, string> pathTitleDeed;
        private Dictionary<string, string> pathMortgagedTitleDeed;
        public IServiceProvider ServiceProvider { get; set; }
        public DiscordSocketClient Client { get; set; }        
        private Dictionary<ulong, List<PreGamePlayer>> preGamePlayersByGuild;
        private Dictionary<ulong, List<DiscordGame>> gamesByGuild;        
        private Dictionary<ActionCard, string> pathActionCards;
        private Dictionary<IUsable, string> imagesPathUsables;
        private int maxGamesPerGuild;

        private Configuration config;
        private DiscordMonopolyConfiguration discordMonopolyConfig;
        internal Secret Secret { get; set; }
        public DataAccessLayer(DiscordSocketClient client, IServiceProvider serviceProvider) : this()
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            ServiceProvider = serviceProvider;
            this.config = serviceProvider.GetRequiredService<Configuration>();
            this.discordMonopolyConfig = new DiscordMonopolyConfiguration(config)
            { Client = this.Client };

        }
        public DataAccessLayer()
        {
            maxGamesPerGuild = 3;
            preGamePlayersByGuild = new();
            gamesByGuild = new();
        }

        public PreGamePlayer[] GetPreGamePlayers(ulong guildId)
        {
            List<PreGamePlayer> preGamePlayers;

            if (!preGamePlayersByGuild.TryGetValue(guildId, out preGamePlayers))
                return null;

            return preGamePlayers.ToArray();
        }
        public void AddPreGamePlayer(ulong guildId, ulong userId, PlayerToken playerToken)
        {
            if (gamesByGuild.ContainsKey(guildId) && gamesByGuild[guildId].Count >= maxGamesPerGuild)
                throw new InvalidOperationException("Number of games limit reached.");
            if (IsUserPlaying(guildId, userId))
                throw new ArgumentException("user ID invalid. user already in a game.");

            var preGamePlayer = new PreGamePlayer(playerToken, userId);
            if (preGamePlayersByGuild.ContainsKey(guildId))
                preGamePlayersByGuild[guildId].Add(preGamePlayer);
            else
            {
                preGamePlayersByGuild.Add(guildId, new List<PreGamePlayer>());
                preGamePlayersByGuild[guildId].Add(preGamePlayer);
            }
            
            var distinctTokensNum = preGamePlayersByGuild[guildId].Distinct().Count();
            if (distinctTokensNum != preGamePlayersByGuild[guildId].Count)
            {
                preGamePlayersByGuild[guildId].Clear();
                throw new Exception("Unknown error occured.");
            }
        }        

        public IEnumerable<DiscordGame> GetGuildGames(ulong guildId)
        {
            if (!gamesByGuild.ContainsKey(guildId))
                throw new ArgumentException("no games found.");
            return gamesByGuild[guildId];
        }
        public DiscordGame GetLastGame(ulong guildId)
        {
            if (!gamesByGuild.ContainsKey(guildId) || !gamesByGuild[guildId].Any())
                throw new ArgumentException("No games found.");

            return gamesByGuild[guildId].LastOrDefault();
        }
        public void AddGame(ulong guildId, Game game = null)
        {
            if (preGamePlayersByGuild[guildId].Count == 0)
                throw new InvalidOperationException("There are no players.");
            if (!gamesByGuild.ContainsKey(guildId))
                gamesByGuild.Add(guildId, new List<DiscordGame>());

            var startingMoney = new int[preGamePlayersByGuild[guildId].Count];
            for ( var i = 0; i < startingMoney.Length; i++ )
            {
                startingMoney[i] = 1500;
            }

            //var config = Game.GetStandardConfiguration(preGamePlayersByGuild[guildId].Select(x => x.PlayerToken).ToArray(), startingMoney);
            //var config = ServiceProvider.GetService<BaseMonopoly.Configurations.Configuration>();
            discordMonopolyConfig.PlayerConfigurations = Game.CreatePlayerConfiguration(preGamePlayersByGuild[guildId].Select(x => x.PlayerToken).ToArray());
            discordMonopolyConfig.PlayerWalletConfigurations = Game.CreatePlayerWalletConfiguration(preGamePlayersByGuild[guildId].Select(x => x.PlayerToken).ToArray(), startingMoney);
            discordMonopolyConfig.PreGamePlayers = preGamePlayersByGuild[guildId];            

            Bank bank = discordMonopolyConfig.LoadBank();
            List<Player> playersPlaying = discordMonopolyConfig.LoadPlayers();
            BaseMonopoly.Assets.BoardAssets.Board board = discordMonopolyConfig.LoadBoard();
            BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets.ColorSet[] colorSets = discordMonopolyConfig.LoadColorSets();
            BaseMonopoly.Assets.BoardAssets.ActionCardAssets.ActionCardDeck communityDeck = discordMonopolyConfig.LoadCommunityDeck();
            BaseMonopoly.Assets.BoardAssets.ActionCardAssets.ActionCardDeck chanceDeck = discordMonopolyConfig.LoadChanceDeck();
            game ??= new Game(bank, playersPlaying, board, colorSets, communityDeck, chanceDeck);
            
            var discordGame = new DiscordGame(game, discordMonopolyConfig.LoadDiscordPlayers(), guildId, (gamesByGuild[guildId].Count + 1).ToString());

            gamesByGuild[guildId].Add(discordGame);
        }
        private bool IsUserPlaying(ulong guildId, ulong userId)
        {
            if (!gamesByGuild.ContainsKey(guildId))
                return false;

            var games = gamesByGuild[guildId];
            var userAlreadyPlaying = false;
            foreach (var game in games)
            {
                if (game.GetDiscordPlayers().Any(x => x.UserID == userId))
                {
                    userAlreadyPlaying = true;
                    break;
                }
            }

            return userAlreadyPlaying;
        }

        public Dictionary<string, string> GetImagesPathTitleDeed()
        {
            if (pathTitleDeed != null)
                return pathTitleDeed;

            var dict = new Dictionary<string, string>();           
            var titleDeeds = new List<TitleDeed>();
            titleDeeds.AddRange(discordMonopolyConfig.LoadStationTitleDeeds());
            titleDeeds.AddRange(discordMonopolyConfig.LoadStreetTitleDeeds());
            titleDeeds.AddRange(discordMonopolyConfig.LoadUtilityTitleDeeds());

            foreach(var td in titleDeeds)
            {
                var path = Path.Combine(TitleDeedsImagesFolder, $"{td.Name.ToLower().Replace(" ", "")}.png");
                dict.Add(td.Name, path);
            }

            this.pathTitleDeed = dict;
            return pathTitleDeed;
        }
        public Dictionary<string, string> GetImagesPathMortgagedTitleDeed()
        {
            if (pathMortgagedTitleDeed != null)
                return pathMortgagedTitleDeed;

            var dict = new Dictionary<string, string>();            
            var titleDeeds = new List<TitleDeed>();
            titleDeeds.AddRange(discordMonopolyConfig.LoadStationTitleDeeds());
            titleDeeds.AddRange(discordMonopolyConfig.LoadStreetTitleDeeds());
            titleDeeds.AddRange(discordMonopolyConfig.LoadUtilityTitleDeeds());

            foreach (var td in titleDeeds)
            {
                var path = Path.Combine(MortgageTitleDeedsImagesFolder, $"{td.Name.ToLower().Replace(" ", "")}_mortgaged.png");
                dict.Add(td.Name, path);
            }

            pathMortgagedTitleDeed = dict;
            return pathMortgagedTitleDeed;
        }
        public Dictionary<ActionCard, string> GetImagesPathActionCard()
        {
            if (pathActionCards != null)
                return pathActionCards;

            var dict = new Dictionary<ActionCard, string>();            
            var actionCards = new List<ActionCard>();
            actionCards.AddRange(discordMonopolyConfig.LoadChanceCard());
            actionCards.AddRange(discordMonopolyConfig.LoadCommunityCards());

            foreach(var ac in actionCards)
            {
                var acConfig = discordMonopolyConfig.ActionCardsConfigurations.Where(x => x.Description == ac.Message).FirstOrDefault();
                var dir = "";
                if (acConfig.CardType.ToLower() == "c")
                {
                    dir = $"{Secret.ActionCardsDirPath}\\ChanceCards";                    
                }
                else
                {
                    dir = $"{Secret.ActionCardsDirPath}\\CommunityChestCards";
                    
                }
                var path = dir + "\\" + acConfig.Id + ".png";
                dict.Add(ac, path);
            }

            this.pathActionCards = dict;
            return pathActionCards;
        }
        public Dictionary<IUsable, string> GetImagesPathUsables()
        {
            if (this.imagesPathUsables != null)
                return imagesPathUsables;
            
            var dict = new Dictionary<IUsable, string>();
            var cards = new List<ActionCard>();
            cards.AddRange(discordMonopolyConfig.LoadChanceCard());
            cards.AddRange(discordMonopolyConfig.LoadCommunityCards());

            foreach(var card in cards)
            {
                var cardConfig = discordMonopolyConfig.ActionCardsConfigurations.Where(x => x.Description == card.Message).FirstOrDefault();
                var dir = "";
                if (cardConfig.CardType.ToLower() == "c")
                {
                    dir = $"{Secret.ActionCardsDirPath}\\ChanceCards";
                }
                else
                {
                    dir = $"{Secret.ActionCardsDirPath}\\CommunityChestCards";

                }
                if (card.Action is IUsable)
                {
                    dict.Add(card.Action as IUsable, dir + "\\" + cardConfig.Id + ".png");
                }
            }

            this.imagesPathUsables = dict;
            return this.imagesPathUsables;
        }
        
    }
}
