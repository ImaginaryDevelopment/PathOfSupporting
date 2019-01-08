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
            Parsing.TreeParsing.GetSkillGem();
            Parsing.TreeParsing.GetSkillGems();
            Parsing.TreeParsing.GetGemReqLevels();
            Console.ReadLine();
        }
    }
}
