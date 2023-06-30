using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Commands.BoardCommands;
using Discord.Interactions;
using MonopolyBoardImageGenerator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fergun.Interactive;
using BaseMonopoly.Commands.TransactionCommands;
using BaseMonopoly.Commands;
using BaseMonopoly.Commands.StreetCommands;
using BaseMonopoly.Commands.ValuableCommands;
using BaseMonopoly.Assets.TransactionAssets;
using BaseMonopoly.Commands.ValuableOrUsableCommands;
using Discord;
using Image = SixLabors.ImageSharp.Image;
using BaseMonopoly.Assets.TransactionAssets.TitleDeedAssets;
using BaseMonopoly.Assets.BoardAssets;

namespace MonopolyDiscordBot
{
    public class GameCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public DataAccessLayer DataAccessLayer { get; set; }        
        public InteractiveService InteractiveService { get; set; }       
        internal Secret Secret { get; set; }
        private DiscordGame GetGame()
        {
            var games = DataAccessLayer.GetGuildGames(Context.Guild.Id);
            var gameWherePlayerIn = games.Where(x => x.GetDiscordPlayers().Any(x => x.UserID == Context.User.Id)).FirstOrDefault() ?? throw new ArgumentException("Game not found.");            
            return gameWherePlayerIn;            
        }
        private DiscordPlayer GetPlayer(DiscordGame game)
        {            
            var player = GetPlayer(game, Context.User);
            return player;
        }
        private DiscordPlayer GetPlayer(DiscordGame game, IUser user)
        {
            var player = game.GetDiscordPlayers().Where(x => x.UserID == user.Id).FirstOrDefault() ?? throw new Exception("Player not found.");
            return player;
        }
        [SlashCommand("testing-transaction", "JUST A TEST COMMAND SO YOU DON'T HAVE TO DO SAME SHT EVERYTIME")]
        public async Task TestMoveMethod()
        {
            DataAccessLayer.AddPreGamePlayer(Context.Guild.Id, Context.User.Id, PlayerToken.black);
            DataAccessLayer.AddPreGamePlayer(Context.Guild.Id, Context.User.Id, PlayerToken.blue);
            DataAccessLayer.AddGame(Context.Guild.Id);
            var game = DataAccessLayer.GetLastGame(Context.Guild.Id);
            game.InitializePlayers();
            game.StartGame();

            await MoveCommand();            
        }
        [SlashCommand("move", "moves player accross the board")]
        public async Task MoveCommand()
        {
            //await DeferAsync();
            var game = GetGame();
            var updater = new RendererUpdater();
            updater.Game = game;
            var player = GetPlayer(game);
            var moveCommand = new MoveCommand(game, player);
            moveCommand.Execute();
            await Respond(game, updater);
        }

        private async Task Respond(DiscordGame game, RendererUpdater updater = null)
        {
            if (updater == null)
            {
                updater = new RendererUpdater();
                updater.Game = game;
            }                

            string tokensBuildingsFolder = Secret.Tokens_BuildingsDirPath;
            using (var boardImage = Image.Load(Secret.BoardPicPath))
            using (var hotelImage = Image.Load(Path.Combine(tokensBuildingsFolder, "Hotel.png")))
            using (var houseImage = Image.Load(Path.Combine(tokensBuildingsFolder, "House.png")))
            using (var redTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_red.png")))
            using (var blueTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_blue.png")))
            using (var blackTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_black.png")))
            using (var greenTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_green.png")))
            using (var pinkTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_pink.png")))
            using (var purpleTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_purple.png")))
            using (var whiteTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_white.png")))
            using (var yellowTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_yellow.png")))
            {

                var dict = new Dictionary<PlayerToken, Image>()
                {
                    { PlayerToken.black, blackTokenImage },
                    { PlayerToken.blue, blueTokenImage },
                    { PlayerToken.red, redTokenImage },
                    { PlayerToken.green, greenTokenImage },
                    { PlayerToken.pink, pinkTokenImage },
                    { PlayerToken.purple, purpleTokenImage },
                    { PlayerToken.white, whiteTokenImage },
                    { PlayerToken.yellow, yellowTokenImage },
                };
                var dictBuilding = new Dictionary<BuildingType, Image>()
                {
                    { BuildingType.Hotel, hotelImage },
                    { BuildingType.House, houseImage },
                };

                var config = JsonConvert.DeserializeObject<ImageGeneratorConfiguration>(File.ReadAllText(Secret.ImageGeneratorConfig));
                var imgGen = new ImageGenerator(boardImage, dict, dictBuilding, game.Board.Spaces.ToArray(), config, game.PlayersPlaying.ToArray());
                var renderer = new DiscordRenderer(DataAccessLayer.GetImagesPathTitleDeed(), DataAccessLayer.GetImagesPathMortgagedTitleDeed(), DataAccessLayer.GetImagesPathActionCard(),
                                                    DataAccessLayer.GetImagesPathUsables(), Context, imgGen, InteractiveService);
                await RespondEphemerally();
                updater.Renderer = renderer;
                await updater.Update();
            }
        }
        private async Task RespondEphemerally()
        {
            await RespondAsync(text: "thinking...", ephemeral: true);
        }        

        [SlashCommand("breakout", "break out from jail")]
        public async Task RollDiceCommand()
        {
            var game = GetGame();            
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var cmd = new BreakOutCommand(game, player);
            cmd.Execute();
            await Respond(game, updater);
        }
        [SlashCommand("endturn", "ends the player's turn")]
        public async Task EndTurn()
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var cmd = new EndTurnCommand(game, player);

            cmd.Execute();
            await Respond(game, updater);
        }
        [SlashCommand("myturn", "your turn now")]
        public async Task MyTurn()
        {
            var game = GetGame();
            var updater = new RendererUpdater(game);


            var cmd = new NextTurnCommand(game);
            cmd.Execute();
            await Respond(game, updater);
        }
        [SlashCommand("pay-fine", "pays fine of jail")]
        public async Task PayFine()
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var cmd = new FineCommand(game, player);
            cmd.Execute();

            await Respond(game, updater);
        }
        [SlashCommand("request-building", "requests a building")]
        public async Task RequestBuilding(BuildingType buildingType)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var cmd = new RequestBuildingCommand(game, player, buildingType);
            cmd.Execute();

            await Respond(game, updater);
        }        
        
        [SlashCommand("bid", "Bids on an auction")]
        public async Task Bid(int amount)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var cmd = new BidCommand(game, player, player.Bank.Auctions.LastOrDefault(), amount);
            cmd.Execute();

            await Respond(game, updater);
        }

        [SlashCommand("trade", "Trades a valuable")]
        public async Task Trade(string name, int amount, IUser user)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            IValuable valuable = player.GetTitleDeeds().Where(x => x.Name == name).FirstOrDefault();
            valuable ??= player.GetUsables().Where(x => x.Name == name).FirstOrDefault();

            if (valuable == null)
                throw new Exception("Valuable not found.");

            var anotherPlayer = GetPlayer(game, user);

            var cmd = new TradeCommand(game, player, valuable, anotherPlayer, amount);
            cmd.Execute();
            await Respond(game, updater);
        }
        [ComponentInteraction("acceptedtransaction*")]
        public async Task AcceptTransaction(string id)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var cmd = new AcceptTransactionCommand(game, player, id);
            cmd.Execute();

            await Respond(game, updater);
        }

        [ComponentInteraction("rejectedtransaction*")]
        public async Task RejectTransaction(string id)
        {
            var game = GetGame();
            var player = GetPlayer(game);            
            var updater = new RendererUpdater(game);
            var cmd = new RejectTransactionCommand(game, player, id);
            cmd.Execute();

            await Respond(game, updater);
        }

        [ComponentInteraction("mortgage*")]
        public async Task Mortgage(string name)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var td = player.GetTitleDeeds().Where(x => x.Name == name).FirstOrDefault();
            var updater = new RendererUpdater(game);
            var cmd = new MortgageCommand(game, player, td);
            cmd.Execute();

            await Respond(game, updater);
        }

        [ComponentInteraction("unmortgage*")]
        public async Task UnMortgage(string name)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var td = player.GetTitleDeeds().Where(x => x.Name == name).FirstOrDefault();
            var updater = new RendererUpdater(game);
            var cmd = new UnMortgageCommand(game, player, td);
            cmd.Execute();

            await Respond(game, updater);
        }

        [ComponentInteraction("updradehouse*")]
        public async Task UpgradeHouse(string name)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var street = game.Board.Spaces.Where(x => x.Name == name).FirstOrDefault();
            if (street is Street st)
            {
                var cmd = new UpgradeHouseStreetCommand(game, player, st);
                cmd.Execute();
            }
            else
                throw new Exception("No Street found.");

            await Respond(game, updater);
        }

        [ComponentInteraction("upgradehotel*")]
        public async Task UpgradeHotel(string name)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var street = game.Board.Spaces.Where(x => x.Name == name).FirstOrDefault();
            if (street is Street st)
            {
                var cmd = new UpgradeHotelStreetCommand(game, player, st);
                cmd.Execute();
            }
            else
                throw new Exception("No Street found.");

            await Respond(game, updater);
        }

        [ComponentInteraction("downgradehouse*")]
        public async Task DowngradeHouse(string name)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var street = game.Board.Spaces.Where(x => x.Name == name).FirstOrDefault();
            if (street is Street st)
            {
                var cmd = new DowngradeHouseStreetCommand(game, player, st);
                cmd.Execute();
            }
            else
                throw new Exception("No Street found.");

            await Respond(game, updater);
        }

        [ComponentInteraction("downgradehotel*")]
        public async Task DowngradeHotel(string name)
        {
            var game = GetGame();
            var player = GetPlayer(game);
            var updater = new RendererUpdater(game);
            var street = game.Board.Spaces.Where(x => x.Name == name).FirstOrDefault();
            if (street is Street st)
            {
                var cmd = new DowngradeHotelStreetCommand(game, player, st);
                cmd.Execute();
            }
            else
                throw new Exception("No Street found.");

            await Respond(game, updater);
        }

        [SlashCommand("wallet", "shows your wallet")]
        public async Task Wallet()
        {
            var game = GetGame();
            var player = GetPlayer(game);

            await RespondAsync("thinking...", ephemeral: true);
            var renderer = new DiscordRenderer(null, InteractiveService, Context);
            await renderer.ShowWallet(player);
        }
        [SlashCommand("show-transactions", "shows transactions")]
        public async Task ShowTransactions(IUser user = null)
        {
            var game = GetGame();
            var player = GetPlayer(game);

            DiscordRenderer renderer = new DiscordRenderer(null, InteractiveService, Context);
            await RespondAsync("thinking...", ephemeral: true);
            await renderer.ShowTransactions(player);
        }

        [SlashCommand("valuable", "shows a valuable")]
        public async Task Valuable(int num, IUser user = null)
        {
            var game = GetGame();
            Player player;
            if (user == null)
                player = GetPlayer(game);
            else
                player = GetPlayer(game, user);

            if (num == 0 || num > player.GetUsables().Length + player.GetTitleDeeds().Length)
                throw new Exception("Invalid number");

            IValuable valuable;
            if (player.GetUsables().Length >= num)
                valuable = player.GetUsables()[num - 1];
            else
                valuable = player.GetTitleDeeds()[num - 1];

            
            var renderer = new DiscordRenderer(null, null, Context);
            renderer.PathUsables = (DataAccessLayer.GetImagesPathUsables());
            renderer.MortgagedTitleDeedPath = DataAccessLayer.GetImagesPathMortgagedTitleDeed();
            renderer.TitleDeedPath = DataAccessLayer.GetImagesPathTitleDeed();
            await RespondAsync("thinking...", ephemeral: true);
            if (valuable is TitleDeed td)
                await renderer.ShowTitleDeed(player, td);
            else
                await renderer.ShowUsable(player, (valuable as IUsable));
        }
        [SlashCommand("transaction", "shows a transaction")]
        public async Task Transaction(string num, IUser user = null)
        {
            var game = GetGame();
            Player player;
            if (user == null)
                player = GetPlayer(game);
            else
                player = GetPlayer(game, user);
            

            var transaction = player.Transactions.Where(x => x.ID == num).FirstOrDefault();
            var renderer = new DiscordRenderer(null, null, Context); 
            renderer.PathUsables = DataAccessLayer.GetImagesPathUsables();
            renderer.MortgagedTitleDeedPath = DataAccessLayer.GetImagesPathMortgagedTitleDeed();
            renderer.TitleDeedPath = DataAccessLayer.GetImagesPathTitleDeed();
            await RespondAsync("thinking...", ephemeral: true);
            await renderer.ShowTransactionInfo(transaction);
        }
        [SlashCommand("board", "shows board")]
        public async Task Board()
        {
            var game = GetGame();
            string tokensBuildingsFolder = Secret.Tokens_BuildingsDirPath;
            using (var boardImage = Image.Load(Secret.BoardPicPath))
            using (var hotelImage = Image.Load(Path.Combine(tokensBuildingsFolder, "Hotel.png")))
            using (var houseImage = Image.Load(Path.Combine(tokensBuildingsFolder, "House.png")))
            using (var redTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_red.png")))
            using (var blueTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_blue.png")))
            using (var blackTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_black.png")))
            using (var greenTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_green.png")))
            using (var pinkTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_pink.png")))
            using (var purpleTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_purple.png")))
            using (var whiteTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_white.png")))
            using (var yellowTokenImage = Image.Load(Path.Combine(tokensBuildingsFolder, "PlayerToken_yellow.png")))
            {

                var dict = new Dictionary<PlayerToken, Image>()
                {
                    { PlayerToken.black, blackTokenImage },
                    { PlayerToken.blue, blueTokenImage },
                    { PlayerToken.red, redTokenImage },
                    { PlayerToken.green, greenTokenImage },
                    { PlayerToken.pink, pinkTokenImage },
                    { PlayerToken.purple, purpleTokenImage },
                    { PlayerToken.white, whiteTokenImage },
                    { PlayerToken.yellow, yellowTokenImage },
                };
                var dictBuilding = new Dictionary<BuildingType, Image>()
                {
                    { BuildingType.Hotel, hotelImage },
                    { BuildingType.House, houseImage },
                };

                var config = JsonConvert.DeserializeObject<ImageGeneratorConfiguration>(File.ReadAllText(Secret.ImageGeneratorConfig));
                var imgGen = new ImageGenerator(boardImage, dict, dictBuilding, game.Board.Spaces.ToArray(), config, game.PlayersPlaying.ToArray());
                var renderer = new DiscordRenderer(DataAccessLayer.GetImagesPathTitleDeed(), DataAccessLayer.GetImagesPathMortgagedTitleDeed(), DataAccessLayer.GetImagesPathActionCard(),
                                                    DataAccessLayer.GetImagesPathUsables(), Context, imgGen, InteractiveService);
                await RespondEphemerally();
                await renderer.ShowBoard(game);
            }
        }
        [SlashCommand("take-rent", "takes rent from a player on your property")]
        public async Task TakeRent(IUser user)
        {
            if (user is null)            
                throw new ArgumentNullException(nameof(user));

            var game = GetGame();
            var player = GetPlayer(game);
            var payer = GetPlayer(game, user);
            var updater = new RendererUpdater(game);
            var rst = game.Board.Spaces.Where(x => x.Name == payer.Space.Name).FirstOrDefault() as IRealStateProperty ?? throw new Exception("An error occured: no real state property found.");
            var cmd = new TakeRentCommand(game, player, payer, rst);
            cmd.Execute();
            await Respond(game, updater);
        }
        [ComponentInteraction("myturn")]
        public async Task MyTurnButton()
        {
            await MyTurn();
        }
        [ComponentInteraction("move")]
        public async Task MoveButton()
        {
            await MoveCommand();
        }
        [ComponentInteraction("end")]
        public async Task EndTurnButton()
        {
            await EndTurn();
        }
        [ComponentInteraction("wallet")]
        public async Task ShowWalletButton()
        {
            await Wallet();
        }
        [ComponentInteraction("transactions")]
        public async Task ShowTransactionsButton()
        {
            await ShowTransactions();
        }
    }
}
