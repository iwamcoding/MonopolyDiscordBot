using BaseMonopoly;
using BaseMonopoly.Assets.BoardAssets.ActionCardAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactionAssets;
using Discord;
using Discord.Interactions;
using MonopolyBoardImageGenerator;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using BaseMonopoly.Assets.TransactionAssets.TitleDeedAssets;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using Image = SixLabors.ImageSharp.Image;
using System.Numerics;
using BaseMonopoly.Assets.TransactionAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StationAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.UtilityAssets;
using Discord.WebSocket;
using System.ComponentModel;

namespace MonopolyDiscordBot
{
    internal class DiscordRenderer
    {
        public ImageGenerator ImageGenerator { get; set; }
        public InteractiveService InteractiveService { get; set; }
        public SocketInteractionContext Context { get; set; }
        public Secret Secret { get; set; }
        public Dictionary<string, string> TitleDeedPath { get; set; }
        public Dictionary<string, string> MortgagedTitleDeedPath { get; set; }
        public Dictionary<ActionCard, string> PathActionCards { get; set; }
        public Dictionary<IUsable, string> PathUsables { get; set; }
        public DiscordRenderer(ImageGenerator imageGenerator, InteractiveService interactiveService, SocketInteractionContext context)
        {
            ImageGenerator = imageGenerator;
            InteractiveService = interactiveService;
            Context = context;
        }

        public DiscordRenderer(Dictionary<string, string> titleDeedPath, Dictionary<string, string> mortgagedTitleDeedPath, Dictionary<ActionCard, string> pathActionCards, Dictionary<IUsable, string> usablesPath, SocketInteractionContext context, ImageGenerator imageGenerator, InteractiveService interactiveService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            PathActionCards = pathActionCards ?? throw new ArgumentNullException(nameof(pathActionCards));
            TitleDeedPath = titleDeedPath ?? throw new ArgumentNullException(nameof(titleDeedPath));
            MortgagedTitleDeedPath = mortgagedTitleDeedPath ?? throw new ArgumentNullException(nameof(mortgagedTitleDeedPath));
            PathUsables = usablesPath ?? throw new ArgumentNullException(nameof(usablesPath));
            ImageGenerator = imageGenerator ?? throw new ArgumentNullException(nameof(imageGenerator));
            InteractiveService = interactiveService ?? throw new ArgumentNullException(nameof(interactiveService));
        }
        public async Task PlayerDrewActionCard(Player player, ActionCard actionCard)
        {
            var filePath = PathActionCards[actionCard];
            var fileName = Path.GetFileName(filePath);

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Action Card Drawn",
                Description = $"{player.PlayerToken} drew {actionCard.Message}",
                ImageUrl = $"attachment://{fileName}"
            };
            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());
        }

        public async Task PlayerJailed(Player player)
        {            
            var filePath = Secret.JailGifPath;

            var fileName = Path.GetFileName(filePath);
            var embedBuilder = new EmbedBuilder()
            {
                Title = "Jailed"
            };
            var fieldBuilder = new EmbedFieldBuilder()
            {
                Name = $"Player Jailed",
                Value = $"{player.PlayerToken} is Jailed"
            };

            embedBuilder.AddField(fieldBuilder);
            embedBuilder.WithImageUrl($"attachment://{fileName}");
            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());
        }
        public async Task PlayerReleased(Player player)
        {            
            var filePath = Secret.ReleaseGifPath;

            var fileName = Path.GetFileName(filePath);
            var embedBuilder = new EmbedBuilder()
            {
                Title = "Released"
            };
            var fieldBuilder = new EmbedFieldBuilder()
            {
                Name = $"Player Released",
                Value = $"{player.PlayerToken} is Released"
            };

            embedBuilder.AddField(fieldBuilder);
            embedBuilder.WithImageUrl($"attachment://{fileName}");
            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());
        }
        public async Task ShowBoard(DiscordGame game)
        {
            var tempPath = "temp.png";
            var embed = GetBoardEmbed(tempPath);

            if (game.CurrentPlayer != null)
            {
                AddPlayerInfo(embed, game);
                var component = GetButtons();
                var embedBuilt = embed.Build();
                await Context.Channel.SendFileAsync(filePath: tempPath, embed: embedBuilt, components: component.Build());
            }
            else
            {
                var myTurnButton = new ButtonBuilder()
                {
                    CustomId = "myturn",
                    Label = "My Turn",
                    Style = ButtonStyle.Primary
                };
                var componentBuilder = new ComponentBuilder()
                {
                    ActionRows = new() { new() { Components = new() { myTurnButton.Build() } } }
                };
                var embedBuilt = embed.Build();
                await Context.Channel.SendFileAsync(filePath: tempPath, embed: embedBuilt, components: componentBuilder.Build());
            }
            
            File.Delete(tempPath);
        }
        private EmbedBuilder GetBoardEmbed(string pathToSave)
        {
            var img = ImageGenerator.GenerateBoardImage();
            img.Save(pathToSave);
            img.Dispose();

            var fileName = Path.GetFileName(pathToSave);
            var embed = new EmbedBuilder()
            {
                Title = "Monopoly Board",
            };
            embed.WithImageUrl($"attachment://{fileName}");
            return embed;
        }
        private void AddPlayerInfo(EmbedBuilder embed, DiscordGame game)
        {
            embed.Title = $"{game.GetDiscordCurrentPlayer().PlayerToken}'s ({game.CurrentPlayer.PlayerToken}) turn.";
            var moneyField = new EmbedFieldBuilder()
            {
                Name = "Money",
                IsInline = true,
            };
            if (game.CurrentPlayer.GetBalance() != 0)
                moneyField.Value = game.CurrentPlayer.GetBalance();
            else
                moneyField.Value = game.CurrentPlayer.GetMoney();
            var titleDeedField = new EmbedFieldBuilder()
            {
                Name = "Valuables",
                Value = game.CurrentPlayer.GetTitleDeeds().Length,
                IsInline = true,
            };
            var transactionField = new EmbedFieldBuilder()
            {
                IsInline = true,
                Name = "Transactions",
                Value = game.CurrentPlayer.Transactions.Count
            };
            embed.WithFields(new List<EmbedFieldBuilder>() { moneyField, titleDeedField, transactionField });
        }
        private ComponentBuilder GetButtons()
        {
            var moveButton = new ButtonBuilder()
            {
                CustomId = "move",
                Label = "Move",
                Style = ButtonStyle.Primary,
            };
            var endTurnButton = new ButtonBuilder()
            {
                CustomId = "end",
                Label = "EndTurn",
                Style = ButtonStyle.Danger
            };
            var walletButton = new ButtonBuilder()
            {
                CustomId = "wallet",
                Label = "Show Wallet",
                Style = ButtonStyle.Secondary
            };
            var transactionButton = new ButtonBuilder()
            {
                CustomId = "transactions",
                Label = "Show Transactions",
                Style = ButtonStyle.Secondary
            };
            var firstRow = new ActionRowBuilder()
            {
                Components = new() { moveButton.Build(), endTurnButton.Build() }
            };
            var secondRow = new ActionRowBuilder()
            {
                Components = new() { walletButton.Build(), transactionButton.Build() }
            };

            var componentBuilder = new ComponentBuilder()
            {
                ActionRows = new() { firstRow, secondRow }
            };

            return componentBuilder;
        }    
        public async Task PlayerMoved(Player player)
        {
            var tempPath = "temp.png";
            var img = ImageGenerator.GenerateBoardImage();
            img.Save(tempPath);
            img.Dispose();            
            var fileName = Path.GetFileName(tempPath);
            var link = $"attachment://{tempPath}";            
            var embed = new EmbedBuilder()
            {
                Title = "Monopoly Board",
                
            };            
            embed.WithImageUrl($"attachment://{fileName}");
            var fieldBuilder = new EmbedFieldBuilder()
            {
                Name = "description",
                Value = $"{player.PlayerToken} moved to {player.Space.Name}"
            };
            embed.WithFields(fieldBuilder);
            var embedBuilt = embed.Build();

            await Context.Channel.SendFileAsync(filePath: tempPath, embed: embedBuilt);            
            File.Delete(tempPath);
        }              
        public async Task PlayerThrewDice(Player player)
        {
            if (player  == null)
                throw new ArgumentNullException(nameof(player));
            if (player.Dice.Any(x => x == null))
                throw new ArgumentException("Dice is null.");
            
            var str = $"{player.PlayerToken} threw dice ";
            for (int i = 0; i < player.Dice.Count; i++)
            {
                if (i > 0)
                    str += $"and {player.Dice.ToArray()[i].Number}";
                else
                    str += $"{player.Dice.ToArray()[i].Number} ";
            }

            var path = Secret.DiceGifDirPath + "\\";
            var nums = player.Dice.OrderBy(x => x.Number).ToArray();
            for (int i = 0; i < nums.Length; i++)
            {
                if (i < nums.Length - 1)
                    path += $"{nums[i].Number}_";
                else
                    path += nums[i].Number;
            }
                
            
            path += ".gif";
            if (!File.Exists(path))
            {
                await Context.Channel.SendMessageAsync("User rolled dice, unknown.");
                return;
            }
                
            var fileName = Path.GetFileName(path);
            var embedBuilder = new EmbedBuilder()
            {
                Title = "Dice Rolled",
                
            };
            var fieldBuilder = new EmbedFieldBuilder()
            {
                Name = "info",
                Value = str
            };

            embedBuilder.AddField(fieldBuilder);
            embedBuilder.WithThumbnailUrl($"attachment://{fileName}");

            await Context.Channel.SendFileAsync(filePath: path, embed: embedBuilder.Build());
        }

        public async Task PlayerTransactionAdded(Transaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }            

            var embedBuilder = new EmbedBuilderTransaction();
            embedBuilder.Transaction = transaction;
            embedBuilder.CreateEmbed();
            embedBuilder.Title = "Transaction Added";
            
            await (Context as SocketInteractionContext).Interaction.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
        public async Task PlayerTransactionRemoved(Player player, TransactionInfo transaction)
        {
            var fieldBuilder = new EmbedFieldBuilder()
            {
                Name = "Type",
                Value = transaction.GetType().Name
            };
            var idFieldBuilder = new EmbedFieldBuilder()
            {
                Name = "ID",
                Value = transaction.TransactionId
            };

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Transaction Completed",
                Description = transaction.Description,
                Fields = new() { fieldBuilder, idFieldBuilder },
                Color = Discord.Color.Red
            };
            await (Context as SocketInteractionContext).Interaction.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
        }        
        public async Task PlayerAddedHouse(Player player)
        {
            var filePath = @$"{Secret.Tokens_BuildingsDirPath}\House.png";
            var fileName = Path.GetFileName(filePath);

            var embedBuilder = new EmbedBuilder()
            {
                Title = "House Recieved",
                Description = $"{player.PlayerToken} recieved a house",
                ThumbnailUrl = $"attachment://{fileName}",
                Color = Discord.Color.Green,
            };

            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());                        
        }
        public async Task PlayerRemovedHouse(Player player)
        {
            var filePath = @$"{Secret.Tokens_BuildingsDirPath}\House.png";
            var fileName = Path.GetFileName(filePath);

            var embedBuilder = new EmbedBuilder()
            {
                Title = "House Removed",
                Description = $"A house was removed from {player.PlayerToken}",
                ThumbnailUrl = $"attachment://{fileName}",
                Color = Discord.Color.Red,
            };

            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());
        }
        public async Task PlayerAddedHotel(Player player)
        {
            var filePath = @$"{Secret.Tokens_BuildingsDirPath}\Hotel.png";
            var fileName = Path.GetFileName(filePath);

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Hotel Recieved",
                Description = $"{player.PlayerToken} recieved a hotel",
                ThumbnailUrl = $"attachment://{fileName}",
                Color = Discord.Color.Green,
            };

            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());
        }
        public async Task PlayerRemovedHotel(Player player)
        {
            var filePath = @$"{Secret.Tokens_BuildingsDirPath}\Hotel.png";
            var fileName = Path.GetFileName(filePath);

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Hotel Removed",
                Description = $"A hotel was removed from {player.PlayerToken}",
                ThumbnailUrl = $"attachment://{fileName}",
                Color = Discord.Color.Green,
            };

            await Context.Channel.SendFileAsync(filePath: filePath, embed: embedBuilder.Build());
        }

        private string GetPath(TitleDeed td)
        {
            if (!td.IsMortgage)
                return TitleDeedPath[td.Name];
            else
                return MortgagedTitleDeedPath[td.Name];
        }
        public async Task ShowTransactionInfo(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            EmbedBuilderTransaction embedBuilder = new EmbedBuilderTransaction();
            embedBuilder.Transaction = transaction;
            embedBuilder.CreateEmbed();
            
            var acceptButton = new ButtonBuilder()
            {
                Label = "Accept",
                Style = ButtonStyle.Success,
                CustomId = $"acceptedtransaction{transaction.ID}"
            };
            var rejectButton = new ButtonBuilder()
            {
                Label = "Reject",
                Style = ButtonStyle.Danger,
                CustomId = $"rejectedtransaction{transaction.ID}"
            };
            if (transaction.TransactionState == TransactionState.close)
            {
                acceptButton.IsDisabled = true; 
                rejectButton.IsDisabled = true;
            }
            var actionRows = new List<ActionRowBuilder>()
            { 
                new ActionRowBuilder()
                {
                    Components = new(){acceptButton.Build(), rejectButton.Build() }
                }
            };
            
            var compBuilder = new ComponentBuilder()
            { 
                ActionRows = actionRows
            };
            

            string? path = null;
            if (transaction is TitleDeedTransaction td)
            {
                path = GetPath(td.Valuable as TitleDeed);
                var fileName = Path.GetFileName(path);
                embedBuilder.ImageUrl = $"attachment://{fileName}";
            }

            if (path != null)
                await Context.Channel.SendFileAsync(filePath: path, embed: embedBuilder.Build(), components: compBuilder.Build());
            else
                await Context.Interaction.FollowupAsync(embed: embedBuilder.Build(), components: compBuilder.Build());
        }
        public async Task ShowTransactions(Player player)
        {
            var transactions = player.Transactions;
            var pagesLimit = Convert.ToInt32(Math.Ceiling(transactions.Count / Convert.ToDecimal(25)));
            PageBuilder[] pages = new PageBuilder[pagesLimit];
            List<EmbedFieldBuilder[]> fieldBuilders = new List<EmbedFieldBuilder[]>();
            var allFields = new EmbedFieldBuilder[transactions.Count];
            //creating fields
            for (int i = 0; i < transactions.Count; i++) 
            {
                var fieldBuilder = new EmbedFieldBuilder()
                {
                    Name = $"{transactions[i].ID}#",
                    Value = EmbedBuilderTransaction.GetStringOfTransaction(transactions[i])
                };
                allFields[i] = fieldBuilder;
            }
            //seperating total fields
            for (int i =0; i < pagesLimit; i++)
            {
                var startingIndex = (i) * 25;
                var endIndex = allFields.Length;

                if (i + 1 < pagesLimit)
                    endIndex = startingIndex + 25;

                fieldBuilders.Add(new EmbedFieldBuilder[endIndex - startingIndex]);
                for (int j = startingIndex; j < endIndex; j++)
                {
                    if (j >= 25)
                        fieldBuilders[i][25 - j] = allFields[j];
                    else
                        fieldBuilders[i][j] = allFields[j];
                }
            }
            //making pages
            for (int i = 0; i < pagesLimit; i++)
            {
                pages[i] = new PageBuilder()
                {
                    Title = "Transaction List",
                    Description = $"Transaction list for {player.PlayerToken}",
                    Fields = fieldBuilders[i].ToList()
                };
            }            
            var paginator = new StaticPaginatorBuilder()
                //.AddUser(Context.User) // Only allow the user that executed the command to interact with the selection.
                .WithPages(pages) // Set the pages the paginator will use. This is the only required component.
                .Build();

            // Send the paginator to the source channel and wait until it times out after 10 minutes.
            await InteractiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));
        }
        public async Task ShowWallet(Player player)
        {            
            var pagesLimit = Convert.ToInt32(Math.Ceiling((player.GetUsables().Length + player.GetTitleDeeds().Length) / Convert.ToDecimal(25)));
            PageBuilder[] pages = new PageBuilder[pagesLimit];
            List<EmbedFieldBuilder[]> fieldBuilders = new List<EmbedFieldBuilder[]>();
            var allFields = new EmbedFieldBuilder[player.GetUsables().Length + player.GetTitleDeeds().Length];
            var k = 0;
            foreach(var item in player.GetUsables()) 
            {
                var fieldBuilder = new EmbedFieldBuilder()
                {
                    Name = $"{k + 1}#",
                    Value = item.Name,
                };
                allFields[k] = fieldBuilder;
                k++;
            }
            foreach (var item in player.GetTitleDeeds())
            {
                var fieldBuilder = new EmbedFieldBuilder()
                {
                    Name = $"{k + 1}#",
                    Value = item.Name,
                };
                allFields[k] = fieldBuilder;
                k++;
            }   
            //seperating total fields
            for (int i = 0; i < pagesLimit; i++)
            {
                var startingIndex = (i) * 25;
                var endIndex = allFields.Length;

                if (i + 1 < pagesLimit)
                    endIndex = startingIndex + 25;

                fieldBuilders.Add(new EmbedFieldBuilder[endIndex - startingIndex]);
                for (int j = startingIndex; j < endIndex; j++)
                {
                    if (j >= 25)
                        fieldBuilders[i][25 - j] = allFields[j];
                    else
                        fieldBuilders[i][j] = allFields[j];
                }
            }
            var moneyToShow = 0;
            if (player.GetBalance() !=  0) 
            {
                moneyToShow = player.GetBalance();
            }
            else
            {
                moneyToShow = player.GetMoney();
            }
            //making pages
            for (int i = 0; i < pagesLimit; i++)
            {                
                pages[i] = new PageBuilder()
                {
                    Title = "Wallet info",
                    Description = $"{player.PlayerToken} has {moneyToShow}",
                    Fields = fieldBuilders[i].ToList()
                };
            }
            
            if (pages.FirstOrDefault() != null)
            {
                var paginator = new StaticPaginatorBuilder()
                //.AddUser(Context.User) // Only allow the user that executed the command to interact with the selection.
                .WithPages(pages) // Set the pages the paginator will use. This is the only required component.
                .Build();

                // Send the paginator to the source channel and wait until it times out after 10 minutes.
                await InteractiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                {
                    Title = "Wallet info",
                    Description = $"{player.PlayerToken} has {moneyToShow}",
                };
                await Context.Interaction.FollowupAsync(embed: embedBuilder.Build());
            }
            
        }
        public async Task ShowTitleDeed(Player player, TitleDeed titleDeed)
        {
            var path = GetPath(titleDeed);
            var fileName = Path.GetFileName(path);
            var desc = "";
            if (titleDeed is StreetTitleDeed)
            {
                var rst = titleDeed.RealStateProperty as Street;
                if (rst.Buildings.Count > 0)
                    desc = $"{titleDeed.Name} has {rst.Buildings.Count} {rst.GetBuildingType()}";
                else
                    desc = $"{titleDeed.Name} has 0 buildings.";
            }
            else if (titleDeed is StationTitleDeed)
            {                
                desc = $"{player.PlayerToken} owns {player.GetTitleDeeds().Where(x => x is StationTitleDeed).Count()} stations";
            }
            else if (titleDeed is UtilityTitleDeed)
            {
                desc = $"{player.PlayerToken} owns {player.GetTitleDeeds().Where(x => x is UtilityTitleDeed).Count()} utilities";
            }

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Title Deed Info",
                Description = desc,
                ImageUrl = $"attachment://{fileName}"
            };

            await Context.Interaction.Channel.SendFileAsync(filePath: path, embed: embedBuilder.Build());
        }
        internal async Task UpdateGame(DiscordGame game)
        {
            var idField = new EmbedFieldBuilder()
            {
                Name = "ID",
                Value = game.ID
            };
            var currentPlayerField = new EmbedFieldBuilder()
            {
                Name = "Current Player"
            };
            if (game.CurrentPlayer != null)
                currentPlayerField.Value = game.CurrentPlayer.PlayerToken;
            else
                currentPlayerField.Value = "Null";

            var nextPlayerField = new EmbedFieldBuilder()
            {
                Name = "Next Player"
            };
            if (game.NextPlayer != null)
                nextPlayerField.Value = game.NextPlayer.PlayerToken;
            else
                nextPlayerField.Value = "Null";            

            var embedBuilder = new EmbedBuilder()
            {
                Title = "Game Info",
                Description = $"Info of game {game.ID}",
                Fields = new() { idField, currentPlayerField, nextPlayerField}
            };

            await (Context as SocketInteractionContext).Interaction.FollowupAsync(embed: embedBuilder.Build());
        }

        internal async Task UpdateBids(DiscordGame game)
        {
            var bids = game.Bank.Auctions;
            var pagesLimit = Convert.ToInt32(Math.Ceiling(bids.Count / Convert.ToDecimal(25)));
            PageBuilder[] pages = new PageBuilder[pagesLimit];
            List<EmbedFieldBuilder[]> fieldBuilders = new List<EmbedFieldBuilder[]>();
            var allFields = new EmbedFieldBuilder[bids.Count];
            //creating fields
            for (int i = 0; i < bids.Count; i++)
            {
                var fieldBuilder = new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}#",
                    Value = bids[i].Transaction.Description
                };
                allFields[i] = fieldBuilder;
            }
            //seperating total fields
            for (int i = 0; i < pagesLimit; i++)
            {
                var startingIndex = (i) * 25;
                var endIndex = allFields.Length;

                if (i + 1 < pagesLimit)
                    endIndex = startingIndex + 25;

                fieldBuilders.Add(new EmbedFieldBuilder[endIndex - startingIndex]);
                for (int j = startingIndex; j < endIndex; j++)
                {
                    if (j >= 25)
                        fieldBuilders[i][25 - j] = allFields[j];
                    else
                        fieldBuilders[i][j] = allFields[j];
                }
            }
            //making pages
            for (int i = 0; i < pagesLimit; i++)
            {
                pages[i] = new PageBuilder()
                {
                    Title = "Auction List",                    
                    Fields = fieldBuilders[i].ToList()
                };
            }
            var paginator = new StaticPaginatorBuilder()
                //.AddUser(Context.User) // Only allow the user that executed the command to interact with the selection.
                .WithPages(pages) // Set the pages the paginator will use. This is the only required component.
                .Build();

            // Send the paginator to the source channel and wait until it times out after 10 minutes.
            await InteractiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));
        }

        internal async Task ShowUsable(Player player, IUsable? usable)
        {
            var path = PathUsables[usable];
            var fileName = Path.GetFileName(path);
            var embedBuilder = new EmbedBuilder()
            {
                Title = usable.Name,
                ImageUrl = $"attachment://{fileName}"
            };
            await Context.Interaction.Channel.SendFileAsync(filePath: path, embed: embedBuilder.Build());
        }

        internal async void UpdateTransaction(Transaction transaction)
        {
            var embed = new EmbedBuilderTransaction()
            {
                Transaction = transaction,
                TitleDeedPath = this.TitleDeedPath
            };
            embed.CreateEmbed();

            embed.Title = "Transaction Updated";
            if (embed.ImageUrl != null)
                await (Context.Interaction.Channel.SendFileAsync(filePath: embed.FilePath, embed: embed.Build()));
            else
                await (Context.Interaction as SocketInteraction).FollowupAsync(embed: embed.Build());
        }

        private class EmbedBuilderTransaction : EmbedBuilder
        {
            public string FilePath { get; set; }
            public Transaction Transaction { get;set; }            
            public Dictionary<string, string> TitleDeedPath { get; set; }
            public Dictionary<string, string> MortgagedTitleDeedPath { get; set; }
            public Dictionary<string, string> UsablePath { get; set; }
            public void CreateEmbed()
            {
                var idField = new EmbedFieldBuilder() { Name = "ID", Value = Transaction.ID };
                var payeeField = new EmbedFieldBuilder() { Name = "Payee", IsInline = true};
                var payerField = new EmbedFieldBuilder() { Name = "Payer", IsInline = true};
                var amountField = new EmbedFieldBuilder() { Name = "Amount", Value = Transaction.Amount, IsInline = true };
                var amountPayedField = new EmbedFieldBuilder() { Name = "Amount Payed", Value = Transaction.AmountPayed.Sum(), IsInline = true };
                var payerAuthorizedFied = new EmbedFieldBuilder() { Name = "Payer Authorized", Value = Transaction.PayerAuthorized};
                var payeeAuthorizedField = new EmbedFieldBuilder() { Name = "Payee Authorized", Value = Transaction.PayeeAuthorized};
                var transactionStateField = new EmbedFieldBuilder() { Name = "Transaction State", Value = Transaction.TransactionState, IsInline = true };
                var transactionStatusFieled = new EmbedFieldBuilder() { Name = "Transaction Status", Value = GetStatus(Transaction.TransactionStatus), IsInline = true };
                var transactionResultField = new EmbedFieldBuilder() { Name = "Transaction Result", Value = Transaction.TransactionResult, IsInline = true };
                var fields = new EmbedFieldBuilder[] { idField, payerField, payeeField, amountField, amountPayedField, payerAuthorizedFied, payeeAuthorizedField, transactionStateField, transactionStatusFieled, transactionResultField };

                this.Title = "Transaction info";
                if (Transaction.Description != null)
                    this.Description = Transaction.Description;
                else
                    this.Description = GetStringOfTransaction(Transaction);

                if (Transaction.Payer is Player p)
                    payerField.WithValue(p.PlayerToken);
                else
                    payerField.WithValue("Bank");

                if (Transaction.Payee is Player pl)
                    payeeField.WithValue(pl.PlayerToken);
                else
                    payeeField.WithValue("Bank");


                this.WithFields(fields);
                if (Transaction is TitleDeedTransaction td && TitleDeedPath != null && MortgagedTitleDeedPath != null)
                {
                    string? path;
                    if (!(td.Valuable as TitleDeed).IsMortgage)
                        path = TitleDeedPath[(td.Valuable.Name)];
                    else
                        path = MortgagedTitleDeedPath[(td.Valuable.Name)];

                    var fileName = Path.GetFileName(path);
                    ImageUrl = $"attachment://{fileName}";
                    FilePath = path;
                }
                else if (Transaction is UsableTransaction us && UsablePath != null)
                {
                    var path = UsablePath[us.Valuable.Name];
                    var fileName = Path.GetFileName(path);
                    ImageUrl = $"attachment://{fileName}";
                    FilePath = path;
                }
                
            }
            public static string GetStringOfTransaction(Transaction transaction)
            {
                var str = "";
                var from = "";
                var to = "";
                var type = "";
                var desc = "";

                if (transaction.Description != null)
                    desc = transaction.Description;

                if (transaction.Payee is Bank)
                    to = "Bank";
                else if (transaction.Payee is Player p)
                    to = p.PlayerToken.ToString();
                else
                    to = "null";

                if (transaction.Payer is Bank)
                    from = "Bank";
                else if (transaction.Payer is Player p)
                    from = p.PlayerToken.ToString();
                else
                    to = "null";

                if (transaction is TitleDeedTransaction td)
                {
                    type = "Title Deed Transaction";
                    desc ??= $"for {(td.Valuable as TitleDeed).Name}";
                }
                else if (transaction is UsableTransaction)
                {
                    type = "Usable Transaction";
                    desc ??= "for a usable";
                }
                else if (transaction is BuildingTransaction bt)
                {
                    type = "Building Transaction";
                    desc ??= $"for {bt.Building.BuildingType}";
                }
                else
                    type = "Transaction";

                str = $"{type}: {from} pays {transaction.Amount} to {to} {desc}";
                return str;
            }
            public static string GetStatus(TransactionStatus status)
            {
                if (status == TransactionStatus.idle) return "Idle";
                if (status == TransactionStatus.cancelled) return "Cancelled";
                if (status == TransactionStatus.pendingAuthorization) return "Pending Authorization";
                return "Pending Authentication";
            }
        }
    }
}
