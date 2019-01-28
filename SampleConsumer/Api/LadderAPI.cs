using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CHelpers;

using PathOfSupporting.Api.Ladder;

namespace SampleConsumer
{
    public static class LadderAPI
    {
        public async static Task GetLadder()
        {
            var result = await Fetch.fetchLadder(LadderArguments.NewWithDetails(new FetchDetails("Standard", null, null, null, null, null, null))).ToTask();
            if(result?.Value != null && result.Value is var ladder)
            {
                Console.WriteLine("Ladder has " +ladder.Total +  (ladder.Total == 1 ? " entry" : " entries"));
                foreach(var e in ladder.Entries)
                {
                    Console.Write(("  " + e.Character.Name + " ").PadRight(20));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Character.Level + " " + e.Character.Class);
                    Console.ResetColor();
                }
            }

        }
    }
}
