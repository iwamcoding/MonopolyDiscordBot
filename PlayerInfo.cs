using BaseMonopoly.Assets.BoardAssets.JailAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    internal record PlayerInfo
    {        
        public int? SpaceNumber { get; set; }
        public bool Jailed { get; set; }
        public bool Moved { get; set; }
        public bool DiceRolled { get; set; }
        public PlayerToken PlayerToken { get; set; }
        public List<Building> Buildings { get; set; }
        public List<TransactionInfo> TransactionInfos { get; set; }
    }
}
