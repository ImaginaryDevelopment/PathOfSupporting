using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CHelpers;

using PathOfSupporting.Api.Stash;

namespace SampleConsumer
{
    public static class StashAPI
    {
        public static void FetchOne()
        {
            var stashes = Fetch.fetchStashes(new FetchArguments(targetUrlOverrideOpt: null,startingChangeIdOpt: null)).ToEnumerable();
            if (stashes.FirstOrDefault()?.Stashes?.FirstOrDefault() is Stash x)
                Console.WriteLine("Stash:" + x);
            else Console.Error.WriteLine("Failed to fetch a stash tab");
        }
        public static void FetchLeagueChangeSets()
        {
            Console.WriteLine("Betrayal starts at " + Fetch.betrayalStart);
            // deferred, IEnumerable is not ToList or anything, and probably shouldn't ever be, this stream is huge.
            var stashes = Fetch.fetchStashes(new FetchArguments(null, Fetch.betrayalStart)).ToEnumerable()
                    .Where(x => x.Stashes.Any(stash => stash.Items.Any(item => item.League?.Contains("Betrayal") == true)));
            // nibble just 5 off the stream
            foreach (var stash in stashes.Take(5))
                Console.WriteLine("Betrayal Stash:" + stash.ChangeId);
        }
        // if you start from 0, it could take a very long time to find the betrayal stashes
        public static void FetchLeagueStashes()
        {
            var stashes = Fetch.fetchStashes(new FetchArguments(null, Fetch.betrayalStart)).ToEnumerable()
                .SelectMany(x => x.Stashes)
                .Where(stash => stash.Items.Any(item => item.League?.Contains("Betrayal") == true));
            foreach (var stash in stashes.Take(5))
                Console.WriteLine("Betrayal Stash:" + stash);
        }

        public static IEnumerable<Stash> FetchDebug()
        {
            foreach (var fetchResult in Impl.fetchStashes(new FetchArguments(null, null)).ToEnumerable())
            {
                if (fetchResult.IsOk)
                {
                    foreach (var stashDebug in fetchResult.ResultValue.Item2.Select(x => x.StashOpt))
                        if (stashDebug.IsOk)
                            yield return stashDebug.ResultValue;
                        else
                        {
                            var err = stashDebug.ErrorValue;
                            var (msg, e) = err;
                            Console.Error.WriteLine("Stash Error:" + msg);
                        }
                }
                else
                {
                    var err = fetchResult.ErrorValue;
                    var (msg, e) = err;
                    Console.Error.WriteLine("Fetch Error:" + msg);
                }

            }
        }
    }
}
