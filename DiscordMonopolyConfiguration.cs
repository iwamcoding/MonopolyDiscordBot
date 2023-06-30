using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StationAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.UtilityAssets;
using BaseMonopoly.Assets.TransactionAssets.TitleDeedAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Assets.TransactionAssets.WalletAssets;
using BaseMonopoly.Configurations;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Configuration = BaseMonopoly.Configurations.Configuration;

namespace MonopolyDiscordBot
{
    public class DiscordMonopolyConfiguration : BaseMonopoly.Configurations.Configuration
    {
        public DiscordSocketClient Client { get; set; }
        public IEnumerable<PreGamePlayer> PreGamePlayers { get; set; }
        private List<DiscordPlayer> discordPlayers;
        private Dictionary<Player, PlayerWallet> playerWallets = new Dictionary<Player, PlayerWallet>();
        public DiscordMonopolyConfiguration(StreetConfiguration[] streetConfigurations, RealStateGroupConfiguration<Street>[] streetGroupConfigurations, 
                                            StreetTitleDeedConfiguration[] streetTitleDeedConfigurations, ColorSetConfiguration[] colorSetConfigurations, 
                                            StationConfiguration[] stationConfigurations, RealStateGroupConfiguration<Station>[] stationGroupConfigurations, 
                                            StationTitleDeedConfiguration[] stationTitleDeedConfigurations, UtilityConfiguration[] utilityConfigurations, 
                                            RealStateGroupConfiguration<Utility>[] utilityGroupConfigurations, UtilityTitleDeedConfiguration[] utilityTitleDeedsConfigurations, 
                                            JailConfiguration jailConfiguration, GoToJailConfiguration goToJailConfiguration, TaxConfiguration[] taxConfigurations, 
                                            GoConfiguration goConfiguration, BankConfiguration bankConfiguration, BankWalletConfiguration bankWalletConfiguration, 
                                            ActionConfiguration[] actionConfigurations, CardsConfiguration[] actionCardsConfiguration, 
                                            ActionCardPlaceConfiguration[] actionPlacesConfiguration, ParkingSpaceConfiguration parkingSpaceConfiguration, 
                                            PlayerConfiguration[] playerConfigurations, PlayerWalletConfiguration[] playerWalletConfigurations) 
            : base(streetConfigurations, streetGroupConfigurations, streetTitleDeedConfigurations, colorSetConfigurations, 
                  stationConfigurations, stationGroupConfigurations, stationTitleDeedConfigurations, utilityConfigurations, 
                  utilityGroupConfigurations, utilityTitleDeedsConfigurations, jailConfiguration, goToJailConfiguration, 
                  taxConfigurations, goConfiguration, bankConfiguration, bankWalletConfiguration, actionConfigurations, 
                  actionCardsConfiguration, actionPlacesConfiguration, parkingSpaceConfiguration, playerConfigurations, playerWalletConfigurations)
        {
        }
        public DiscordMonopolyConfiguration(Configuration config) : base(config.StreetConfigurations, config.StreetGroupConfigurations, config.StreetTitleDeedConfigurations,
                                                                        config.ColorSetConfigurations, config.StationConfigurations, config.StationGroupConfigurations, config.StationTitleDeedConfigurations,
                                                                        config.UtilityConfigurations, config.UtilityGroupConfigurations, config.UtilityTitleDeedsConfigurations, config.JailConfiguration,
                                                                        config.GoToJailConfiguration, config.TaxConfigurations, config.GoConfiguration, config.BankConfiguration, config.BankWalletConfiguration,
                                                                        config.ActionConfigurations, config.ActionCardsConfigurations, config.ActionPlacesConfiguration, config.ParkingSpaceConfiguration,
                                                                        config.PlayerConfigurations, config.PlayerWalletConfigurations)
        {

        }
        public override List<Player> LoadPlayers()
        {
            if (PlayerConfigurations == null)
                throw new InvalidOperationException("Cannot load players.");
            if (PlayerWalletConfigurations == null)
                throw new InvalidOperationException("Cannot load players.");
            if (this.players != null)
                return players;

            var playersToReturn = new List<Player>();
            var index = 0;
            foreach (var config in PlayerConfigurations)
            {
                var walletConfig = PlayerWalletConfigurations.Where(x => x.PlayerToken == config.PlayerToken).FirstOrDefault();
                var wallet = new PlayerWallet(walletConfig.Money);
                var titleDeeds = new List<TitleDeed>();
                titleDeeds.AddRange(LoadStreetTitleDeeds());
                titleDeeds.AddRange(LoadStationTitleDeeds());
                titleDeeds.AddRange(LoadUtilityTitleDeeds());
                if (walletConfig.TitleDeeds != null)
                {
                    foreach (var name in walletConfig.TitleDeeds)
                    {
                        wallet.AddValuable(titleDeeds.Where(x => x.Name == name).FirstOrDefault());
                    }
                }
                playersToReturn.Add(new Player(wallet, LoadBank(), config.PlayerToken, 1));
                playerWallets.Add(playersToReturn[index], wallet);
                index++;
            }

            this.players = playersToReturn;
            return playersToReturn;
        }

        internal List<DiscordPlayer> LoadDiscordPlayers()
        {
            if (discordPlayers != null) return discordPlayers;

            discordPlayers = new List<DiscordPlayer>();
            var players = LoadPlayers();
            foreach (var player in players)
            {
                var preGamePlayer = PreGamePlayers.Where(x => x.PlayerToken == player.PlayerToken).FirstOrDefault();                
                discordPlayers.Add(new DiscordPlayer(playerWallets[player], player.Bank, player.PlayerToken, player.SpaceNumber)
                {
                    UserID = preGamePlayer.UserId,                    
                    Username = Client.GetUser(preGamePlayer.UserId).Username
                });
            }

            return discordPlayers;
        }
    }
}
