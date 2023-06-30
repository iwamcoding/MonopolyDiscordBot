using BaseMonopoly;
using BaseMonopoly.Assets.BoardAssets;
using BaseMonopoly.Assets.BoardAssets.ActionCardAssets;
using BaseMonopoly.Assets.BoardAssets.RealStateAssets.StreetAssets;
using BaseMonopoly.Assets.TransactionAssets.TransactableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    public class DiscordGame : Game
    {
        public ulong GuildID { get; set; }
        public string ID { get; set; }                
        public DiscordGame(Game game, List<DiscordPlayer> discordPlayersPlaying, ulong guildID, string id) : base(game.Bank, game.PlayersPlaying, game.Board, game.ColorSets, game.CommunityChestDeck, game.ChanceDeck)
        {
            if (discordPlayersPlaying.Count != game.PlayersPlaying.Count)
                throw new InvalidOperationException("Players invalid.");
            this.GuildID = guildID;
            this.ID = id;

            for (int i = 0; i < this.PlayersPlaying.Count; i++)
            {
                this.PlayersPlaying[i] = discordPlayersPlaying[i];
            }            
        }
        public List<DiscordPlayer> GetDiscordPlayers()
        {
            var list = new List<DiscordPlayer>();
            foreach (var player in this.PlayersPlaying) 
            {
                list.Add(player as DiscordPlayer);
            }
            return list;
        }
        public DiscordPlayer GetDiscordCurrentPlayer()
        {
            if (this.CurrentPlayer == null)
                return null;

            var players = GetDiscordPlayers();
            var player = players.Where(x => x.PlayerToken == this.CurrentPlayer.PlayerToken).FirstOrDefault();
            return player;
        }
    }
}
