using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PathOfSupporting.StashAPI;

namespace SampleConsumer
{
    public static class StashAPI
    {
        public static void FetchOne()
        {
            if (Fetch.fetchStashes(null,null).FirstOrDefault()?.Stashes?.FirstOrDefault() is Stash x)
                Console.WriteLine("Stash:" + x);
            else Console.Error.WriteLine("Failed to fetch a stash tab");
        }
        // starts from 0, could take a very long time to find the betrayal stashes
        public static void FetchLeagueStashes()
        {
            var stashes = Fetch.fetchStashes(null,null).SelectMany(x => x.Stashes).Where(stash => stash.Items.Any(item => item.League == "Betrayal"));
            foreach (var stash in stashes.Take(5))
                Console.WriteLine("Betrayal Stash:" + stash);
        }

        public static IEnumerable<Stash> FetchDebug()
        {
            foreach (var fetchResult in Impl.fetchStashes(null,null).SelectMany(x => x.Item2))
            {
                if (fetchResult.StashOpt.IsOk && fetchResult.StashOpt.ResultValue is Stash x)
                    yield return x;
                else
                {
                    var err = fetchResult.StashOpt.ErrorValue;
                    var (msg, e) = err;
                    Console.Error.WriteLine("Stash Error:" + msg);
                }

            }
        }
    }
}
