using BaseMonopoly.Assets.TransactionAssets.TransactionAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    internal record TransactionInfo
    {
        public string TransactionId { get; set; }
        public string Description { get;set; }
        public bool PayerAuthorized { get; set; }
        public bool PayeeAuthorized { get; set; }
        public int AmountPayed { get; set; }
        public TransactionState TransactionState { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public TransactionResult Result { get; set; }
    }
}
