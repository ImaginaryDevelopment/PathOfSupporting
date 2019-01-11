using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            ItemParsing.ParseItem();
            Parsing.TreeParsing.Gems.GetSkillGem();
            Parsing.TreeParsing.Gems.GetSkillGems();
            Parsing.TreeParsing.Gems.GetGemReqLevels();
            Parsing.TreeParsing.Passives.DecodeUrl();
            //Parsing.TreeParsing.PathOfBuilding.ParsePastebin();
            Parsing.TreeParsing.PathOfBuilding.ParseCode();
            Parsing.TreeParsing.PathOfBuilding.ParseMinionPasteBin();
            StashAPI.FetchOne();
            StashAPI.FetchLeagueStashes();
            StashAPI.FetchLeagueChangeSets();
            Console.WriteLine();
            Console.WriteLine("Done...");
            Console.ReadLine();
        }
    }
}
