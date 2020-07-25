using System;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace PruningRadixTrie.Benchmark
{

    class Program
    {
        public static void Benchmark()
        {
            Console.WriteLine("Load dictionary & create trie ...");
            PruningRadixTrie pruningRadixTrie = new PruningRadixTrie();
            if (!File.Exists("terms.txt"))
            {
                ZipFile.ExtractToDirectory("terms.zip", ".");
            }
            pruningRadixTrie.ReadTermsFromFile("terms.txt");

            Console.WriteLine("Benchmark started ...");
            int rounds = 1000;
            string queryString = "microsoft";
            for (int i = 0; i < queryString.Length; i++)
            {
                //benchmark Ordinary Radix Trie
                Stopwatch sw = Stopwatch.StartNew();
                for (int loop = 0; loop < rounds; loop++)
                {
                    var results=pruningRadixTrie.GetTopkTermsForPrefix(queryString.Substring(0, i + 1), 10,out long termFrequencyCountPrefix, false);
                    //foreach ((string term, long termFrequencyCount) in results) Console.WriteLine(term + " " + termFrequencyCount.ToString("N0"));
                }
                sw.Stop();
                long time1 = sw.ElapsedMilliseconds;
                Console.WriteLine("ordinary search " + queryString.Substring(0, i + 1) + " in " + ((double)time1 / (double)rounds).ToString("N6") + " ms");
                

                //benchmark Pruning Radix Trie
                sw = Stopwatch.StartNew();
                for (int loop = 0; loop < rounds; loop++)
                {
                    var results = pruningRadixTrie.GetTopkTermsForPrefix(queryString.Substring(0, i + 1), 10, out long termFrequencyCountPrefix, true);
                    //foreach ((string term,long termFrequencyCount) in results) Console.WriteLine(term+" "+termFrequencyCount.ToString("N0"));
                }
                sw.Stop();
                long time2 = sw.ElapsedMilliseconds;
                Console.WriteLine("pruning search " + queryString.Substring(0, i + 1) + " in " + ((double)time2 / (double)rounds).ToString("N6") + " ms");
                

                Console.WriteLine(((double)time1 / (double)time2).ToString("N2") + " x faster");
            }

            Console.WriteLine("press key to exit.");
            Console.ReadKey();
        }


        static void Main(string[] args)
        {
            Benchmark();      
        }
    }
}
