using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    public class PreGamePlayer
    {        
        public PlayerToken PlayerToken { get; set; }
        public ulong UserId { get; set; }
        public PreGamePlayer()
        {
        }

        public PreGamePlayer(PlayerToken playerToken, ulong userId)
        { 
            PlayerToken = playerToken;
            UserId = userId;
        }
    }
}
