using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PathOfSupporting.Parsing.Html;
using CHelpers;

namespace SampleConsumer.Parsing
{
    public static class HtmlParsing
    {
        public static void GetCharacters() => GetCharactersTask().GetAwaiter().GetResult();

        static async Task GetCharactersTask()
        {
            var result = await PathOfExile.Com.getCharacters("DevelopersDevelopersDevelopers").ToTask();
            if(result.IsSuccess && result.GetSuccess().Value is var characters)
            {
                Console.WriteLine("Characters fetched");
                foreach (var ch in characters)
                    Console.WriteLine("  " + ch.Name.PadRight(25) + " " + ch.Level.ToString().PadRight(3) + " " + ch.Class);
            }
        }
    }
}
