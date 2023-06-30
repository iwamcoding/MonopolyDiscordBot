using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BaseMonopoly.Assets.BoardAssets.ActionCardAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets;
using BaseMonopoly.Assets.TransactionAssets.TitleDeedAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactionAssets;
using BaseMonopoly.Assets.TransactionAssets.WalletAssets;

namespace MonopolyDiscordBot
{
    internal class RendererUpdater
    {
        public DiscordRenderer Renderer { get; set; }
        private DiscordGame game;
        public DiscordGame Game { get { return game; } set { game = value; CopyValues(); } }

        private Player recordedCurrentPlayer;
        private Player recordedNextPlayer;
        private int recoredBidCount;
        private bool bidExists;

        private List<PlayerInfo> playersInfo;                             

        private Dictionary<Street, List<Building>> streetsBuildings;
        private Dictionary<TitleDeed, bool> titleDeedMortgaged;                        
        
        public RendererUpdater()
        {
        }

        public RendererUpdater(DiscordRenderer renderer, DiscordGame game)
        {
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            Game = game ?? throw new ArgumentNullException(nameof(game));            
        }
        
        public RendererUpdater(DiscordGame game)
        {
            Game = game ?? throw new ArgumentNullException( nameof(game));
        }
        private void CopyValues()
        {
            playersInfo = new List<PlayerInfo>();            
            streetsBuildings = new();
            titleDeedMortgaged = new();

            var players = Game.PlayersPlaying;
            foreach (var player in players)            
            {
                bool diceRolled = true;
                if (player.GetDiceSum() == null)
                    diceRolled = false;

                var buildings = new List<Building>();
                foreach(var building in player.Buildings)
                {
                    buildings.Add(building);
                }

                playersInfo.Add(new PlayerInfo()
                {
                    PlayerToken = player.PlayerToken,
                    DiceRolled = diceRolled,
                    Jailed = player.IsJailed,
                    Moved = player.Moved,
                    Buildings = buildings,
                    TransactionInfos = GetTransactionsInfo(player.Transactions),
                    SpaceNumber = player.SpaceNumber
                });

                

                foreach(var titleDeed in player.GetTitleDeeds())
                {
                    titleDeedMortgaged.Add(titleDeed, titleDeed.IsMortgage);
                }
            }

            var streets = Game.Board.Spaces.Where(x => x is Street).Select(x => x as Street);
            foreach(var street in streets)
            {
                var buildings = new List<Building>();
                buildings.AddRange(street.Buildings);
                streetsBuildings.Add(street, buildings);
            }

            recordedCurrentPlayer = game.CurrentPlayer;
            recordedNextPlayer = game.NextPlayer;
            recoredBidCount = game.Bank.Auctions.Count;
        }

        public async Task Update()
        {
            if (Renderer == null)
                throw new InvalidOperationException("Renderer unavailable.");
            if (Game == null)
                throw new InvalidOperationException("Game not provided.");
            
            await UpdatePlayers();
            await UpdateTransactions();
            await UpdateGame();
        }

        private async Task UpdatePlayers()
        {
            foreach(var player in Game.PlayersPlaying) 
            {
                var playerInfo = playersInfo.Where(x => x.PlayerToken == player.PlayerToken).FirstOrDefault() ?? throw new Exception("An error occured.");

                bool diceRolled;
                if (player.GetDiceSum() == null)
                    diceRolled = false;
                else
                    diceRolled = true;

                if (!playerInfo.DiceRolled && diceRolled) 
                {
                    await Renderer.PlayerThrewDice(player);                    
                }
                

                if (playerInfo.Jailed != player.IsJailed) 
                {
                    if (player.IsJailed)
                        await Renderer.PlayerJailed(player);
                    else
                        await Renderer.PlayerReleased(player);                    
                }
                

                if (playerInfo.Moved != player.Moved) 
                {
                    if (player.Moved)
                    {                        
                        await Renderer.PlayerMoved(player);                        
                        if (player.Space is ActionCardPlace p)
                        {
                            await Renderer.PlayerDrewActionCard(player, p.CardDeck.GetLastDrawnCard());
                        }
                        else
                        {
                            var actionPlaces = game.Board.Spaces.Where(x => x is ActionCardPlace);
                            if ((player.Space is not ActionCardPlace) && actionPlaces.Any(x => x.SpaceNumber == (player.GetDiceSum() + playerInfo.SpaceNumber)))
                            {
                                var place = actionPlaces.Where(x => x.SpaceNumber == playerInfo.SpaceNumber + player.GetDiceSum()).FirstOrDefault() as ActionCardPlace;
                                if (player == null)
                                    throw new Exception("An error occured.");

                                await Renderer.PlayerDrewActionCard(player, place.CardDeck.GetLastDrawnCard());
                            }

                        }
                    }
                        
                }                

                if (player.Buildings.Count != playerInfo.Buildings.Count)
                {
                    var currentHouseCount = player.Buildings.Where(x => x.BuildingType == BuildingType.House).Count();
                    var currentHotelCount = player.Buildings.Where(x => x.BuildingType == BuildingType.Hotel).Count();

                    var prevHouseCount = playerInfo.Buildings.Where(x => x.BuildingType == BuildingType.House).Count();
                    var prevHotelCount = playerInfo.Buildings.Where(x => x.BuildingType == BuildingType.Hotel).Count();

                    if (currentHouseCount > prevHouseCount) 
                    {
                        await Renderer.PlayerAddedHouse(player);
                    }
                    else if (currentHouseCount < prevHouseCount) 
                    {
                        await Renderer.PlayerRemovedHouse(player);
                    }

                    if (currentHotelCount > prevHotelCount)
                    {
                        await Renderer.PlayerAddedHotel(player);
                    }
                    else if (currentHotelCount  < prevHotelCount)
                    {
                        await Renderer.PlayerRemovedHotel(player);
                    }
                }
            }
        }
        private async Task UpdateTransactions()
        {
            foreach(var player in Game.PlayersPlaying) 
            {
                var prevTransactionInfos = playersInfo.Where(x => x.PlayerToken == player.PlayerToken).FirstOrDefault().TransactionInfos ?? throw new Exception("An error occured.");
                var currentTransactionInfos = GetTransactionsInfo(player.Transactions);

                if (prevTransactionInfos.Count > currentTransactionInfos.Count) 
                {
                    var removedTransactions = prevTransactionInfos.Except(currentTransactionInfos);
                    foreach (var transactionInfo in removedTransactions) 
                    {
                        await Renderer.PlayerTransactionRemoved(player, transactionInfo);
                    }
                }
                else if (prevTransactionInfos.Count < currentTransactionInfos.Count)
                {
                    var addedTransactions = currentTransactionInfos.Except(prevTransactionInfos);
                    foreach(var  transactionInfo in addedTransactions)
                    {
                        await Renderer.PlayerTransactionAdded(player.Transactions.Where(x => x.ID == transactionInfo.TransactionId).FirstOrDefault());
                    }
                }
                else
                {
                    foreach(var info in prevTransactionInfos) 
                    {
                        var currentInfo = currentTransactionInfos.Where(x => x.TransactionId == info.TransactionId).FirstOrDefault();

                        if (currentInfo == null)
                            continue;

                        if (currentInfo != info)
                            Renderer.UpdateTransaction(player.Transactions.Where(x => x.ID == info.TransactionId).FirstOrDefault());
                    }
                }                
            }
        }
        private async Task UpdateGame()
        {
            if (recordedCurrentPlayer != game.CurrentPlayer)
                await Renderer.UpdateGame(game);
            
            if (game.Bank.Auctions.Count != recoredBidCount)
                await Renderer.UpdateBids(game);
        }
        private List<TransactionInfo> GetTransactionsInfo(IEnumerable<Transaction> transactions)
        {
            var infos = new List<TransactionInfo>();
            foreach (var transaction in transactions)
            {
                infos.Add(new TransactionInfo()
                {
                    TransactionId = transaction.ID,
                    Description = transaction.Description,
                    PayeeAuthorized = transaction.PayeeAuthorized,
                    PayerAuthorized = transaction.PayeeAuthorized,
                    Result = transaction.TransactionResult,
                    TransactionState = transaction.TransactionState,
                    TransactionStatus = transaction.TransactionStatus,
                    AmountPayed = transaction.AmountPayed.Sum()
                });
            }

            return infos;
        }
    }
}
