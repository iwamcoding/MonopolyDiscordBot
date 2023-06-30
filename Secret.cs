using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonopolyDiscordBot
{
    internal class Secret
    {
        public string BotToken { get; set; }
        public string AssetsDirPath { get; set; }
        public string ActionCardsDirPath { get; set; }
        public string DiceGifDirPath { get; set; }
        public string Tokens_BuildingsDirPath { get; set; }
        public string TitleDeedDirPath { get; set; }
        public string JailGifPath { get; set; }
        public string ReleaseGifPath { get; set; }
        public string BoardPicPath { get; set; }
        public string ImageGeneratorConfig { get;set; }
        public Secret(string token, string assetsDirPath, string actionCardsDirPath, string diceGifDirPath, string tokens_BuildingsDirPath, 
                    string titleDeedDirPath, string jailGifPath, string releaseGifPath, string boardPicPath)
        {
            BotToken = token;
            AssetsDirPath = assetsDirPath;            
            ActionCardsDirPath = actionCardsDirPath;
            DiceGifDirPath = diceGifDirPath;
            Tokens_BuildingsDirPath = tokens_BuildingsDirPath;
            TitleDeedDirPath = titleDeedDirPath;
            JailGifPath = jailGifPath;
            ReleaseGifPath = releaseGifPath;
            BoardPicPath = boardPicPath;
        }
    }
}
