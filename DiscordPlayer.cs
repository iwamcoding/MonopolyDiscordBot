using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using BaseMonopoly.Assets.TransactionAssets.WalletAssets;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    public class DiscordPlayer : Player
    {
        public DiscordPlayer(PlayerWallet wallet, Bank bank, PlayerToken playerToken, int? spaceNumber) : base(wallet, bank, playerToken, spaceNumber)
        {
        }
        public string Username { get; set; }
        public ulong UserID { get; set; }
        
    }
}
