using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace PruningRadixTrie
{

    /// <summary>
    /// Summary description for Trie
    /// </summary>
    public class AutocompleteRadixtrie
    {
        public long termCount = 0;
        public long termCountLoaded = 0;

        //Trie node class
        public class Node
        {
            public List<(string, Node)> Children;

            //Does this node represent the last character in a word? 
            //0: no word; >0: is word (wordcount)
            public long wordCount;
            public long wordCountChildMax;

            public Node(long count)
            {
                wordCount = count;
            }
        }

        //The trie
        private Node trie;

        public AutocompleteRadixtrie()
        {
            trie = new Node(0);
        }

        // Insert a word into the trie
        public void AddTerm(String s, long count)
        {
            List<Node> nodeList = new List<Node>();
            AddTerm(trie, s, count, 0, 0, nodeList);
        }

        public void updateMaxCounts(List<Node> nodeList, long wordCount)
        {
            foreach (Node node in nodeList) if (wordCount > node.wordCountChildMax) node.wordCountChildMax = wordCount;
        }

        public void AddTerm(Node curr, String s, long count, int id, int level, List<Node> nodeList)
        {
            try
            {
                nodeList.Add(curr);

                //test for common prefix (with possibly different suffix)
                int common = 0;
                string oldKey = "";
                Node oldNode = new Node(0);
                if (curr.Children != null) for (int j = 0; j < curr.Children.Count; j++)
                    {
                        (string, Node) kvp = curr.Children[j];
                        oldKey = kvp.Item1;
                        oldNode = kvp.Item2;

                        for (int i = 0; i < Math.Min(s.Length, oldKey.Length); i++) if (s[i] == oldKey[i]) common = i + 1; else break;

                        if (common > 0)
                        {
                            //term already existed
                            //existing ab
                            //new      ab
                            if ((common == s.Length) && (common == oldKey.Length))
                            {
                                if (oldNode.wordCount == 0) termCount++;
                                oldNode.wordCount += count;
                                updateMaxCounts(nodeList, oldNode.wordCount);
                            }
                            //new is subkey
                            //existing abcd
                            //new      ab
                            //if new is shorter (== common), then node(count) and only 1. children add (clause2)
                            else if (common == s.Length)
                            {
                                //insert second part of oldKey as child 
                                Node child = new Node(count);
                                child.Children = new List<(string, Node)>
                                {
                                   (oldKey.Substring(common), oldNode)
                                };
                                child.wordCountChildMax = Math.Max(oldNode.wordCountChildMax, oldNode.wordCount);
                                updateMaxCounts(nodeList, count);

                                //insert first part as key, overwrite old node
                                curr.Children[j] = (s.Substring(0, common), child);
                                //increment termcount by 1
                                termCount++;

                            }
                            //if oldkey shorter (==common), then recursive addTerm (clause1)
                            //existing: te
                            //new:      test
                            else if (common == oldKey.Length)
                            {
                                AddTerm(oldNode, s.Substring(common), count, id, level + 1, nodeList);
                            }
                            //old and new have common substrings
                            //existing: test
                            //new:      team
                            else
                            {
                                //insert second part of oldKey and of s as child 
                                Node child = new Node(0);//count       
                                child.Children = new List<(string, Node)>
                                {
                                     (oldKey.Substring(common), oldNode) ,
                                     (s.Substring(common), new Node(count))
                                };
                                child.wordCountChildMax = Math.Max(oldNode.wordCountChildMax, Math.Max(count, oldNode.wordCount));
                                updateMaxCounts(nodeList, count);

                                //insert first part as key. overwrite old node
                                curr.Children[j] = (s.Substring(0, common), child);
                                //increment termcount by 1 
                                termCount++;
                            }
                            return;
                        }
                    }

                // initialize dictionary if first key is inserted 
                if (curr.Children == null)
                {
                    curr.Children = new List<(string, Node)> 
                        {
                            ( s, new Node(count) )
                        };
                }
                else
                {
                    curr.Children.Add((s, new Node(count)));
                }
                termCount++;
                updateMaxCounts(nodeList, count);
            }
            catch (Exception e) { Console.WriteLine("exception: " + s + " " + e.Message); }
        }

        public void FindAllChildTerms(String prefix, int topK, ref long prefixCount, string prefixString, List<(string, long)> results, bool pruning)
        {
            FindAllChildTerms(prefix, trie, topK, ref prefixCount, prefixString, results, null,pruning);
        }

        public void FindAllChildTerms(String prefix, Node curr, int topK, ref long prefixCount, string prefixString, List<(string, long)> results, System.IO.StreamWriter file, bool pruning)
        {
            try
            {
                //pruning/early termination in radix trie lookup
                if (pruning && (topK > 0) && (results.Count == topK) && (curr.wordCountChildMax <= results[topK - 1].Item2)) return;

                //test for common prefix (with possibly different suffix)
                string oldKey = "";
                Node oldNode = new Node(0);
                bool noPrefix = string.IsNullOrEmpty(prefix);

                if (curr.Children != null) foreach ((string, Node) kvp in curr.Children)
                    {

                        //pruning/early termination in radix trie lookup
                        if (pruning && (topK > 0) && (results.Count == topK) && (kvp.Item2.wordCount <= results[topK - 1].Item2) && (kvp.Item2.wordCountChildMax <= results[topK - 1].Item2))
                        {
                            if (!noPrefix) break; else continue;
                        }

                        oldKey = kvp.Item1;
                        oldNode = kvp.Item2;
                        if (noPrefix || oldKey.StartsWith(prefix))
                        {
                            if (oldNode.wordCount > 0)
                            {
                                if (prefix == oldKey) prefixCount = oldNode.wordCount;

                                //candidate
                                if (file != null) file.WriteLine(prefixString + oldKey + "\t" + oldNode.wordCount.ToString());
                                else
                                if (topK > 0) AddTopKSuggestion(prefixString + oldKey, oldNode.wordCount, topK, ref results); else results.Add((prefixString + oldKey, oldNode.wordCount));
                            }

                            if ((oldNode.Children != null) && (oldNode.Children.Count > 0)) FindAllChildTerms("", oldNode, topK, ref prefixCount, prefixString + oldKey, results, file,pruning);
                            if (!noPrefix) break;
                        }
                        else if (prefix.StartsWith(oldKey))
                        {

                            if ((oldNode.Children != null) && (oldNode.Children.Count > 0)) FindAllChildTerms(prefix.Substring(oldKey.Length), oldNode, topK, ref prefixCount, prefixString + oldKey, results, file,pruning);
                            break;
                        }
                    }

            }
            catch (Exception e) { Console.WriteLine("exception: " + prefix + " " + e.Message); }
        }

        public List<(string, long)> GetTermsForPrefix(String prefix, int topK, out long prefixCount, bool pruning)
        {
            List<(string, long)> results = new List<(string, long)>();
            prefixCount = 0;

            // At the end of the prefix, find all child words
            FindAllChildTerms(prefix, topK, ref prefixCount, "", results,pruning);

            return results;
        }


        public void WriteTermsToFile(string path)
        {
            //save only if new terms were added
            if (termCountLoaded == termCount) return;
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
                {
                    long prefixCount = 0;
                    FindAllChildTerms("", trie, 0, ref prefixCount, "", null, file,true);
                }
                Console.WriteLine(termCount.ToString("N0") + " terms written.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Writing terms exception: " + e.Message);
            }
        }

        public bool ReadTermsFromFile(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine("Could not find file " + path);
                return false;
            }
            try
            {
                Stopwatch sw1 = Stopwatch.StartNew();
                using (System.IO.Stream corpusStream = System.IO.File.OpenRead(path))
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(corpusStream, System.Text.Encoding.UTF8, false))
                    {
                        String line;

                        //process a single line at a time only for memory efficiency
                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] lineParts = line.Split("\t");
                            if (lineParts.Length == 2)
                            {
                                if (Int64.TryParse(lineParts[1], out Int64 count))
                                {
                                    this.AddTerm(lineParts[0], count);
                                }
                            }
                        }

                    }
                }
                termCountLoaded = termCount;
                Console.WriteLine(termCount.ToString("N0") + " terms loaded in " + sw1.ElapsedMilliseconds.ToString("N0") + " ms");
            }
            catch (Exception e)
            {
                Console.WriteLine("Loading terms exception: " + e.Message);
            }

            return true;
        }

        public class BinarySearchComparer : IComparer<(string, long)>
        {
            public int Compare((string, long) f1, (string, long) f2)
            {
                return Comparer<long>.Default.Compare(f2.Item2, f1.Item2);//descending
            }
        }

        public void AddTopKSuggestion(string word, long count, int topK, ref List<(string word, long count)> results)
        {
            //at the end/highest index is the lowest value
            // >  : old take precedence for equal rank   
            // >= : new take precedence for equal rank 
            if ((results.Count < topK) || (count >= results[topK - 1].count))
            {
                int index = results.BinarySearch((word, count), new BinarySearchComparer());
                if (index < 0) results.Insert(~index, (word, count)); else results.Insert(index, (word, count));

                if (results.Count > topK) results.RemoveAt(topK);
            }

        }

    }



    class Program
    {
        public static void Benchmark()
        {
            string path = "terms.txt";
            Console.WriteLine("Load dictionary & create trie ...");
            AutocompleteRadixtrie suggestionRadixtrie = new AutocompleteRadixtrie();
            suggestionRadixtrie.ReadTermsFromFile(path);
            Console.WriteLine("Benchmark started ...");

            string queryString = "microsoft";
            List<(string, long)> results = new List<(string, long)>();
            int rounds = 1000;

            for (int i = 0; i < queryString.Length; i++)
            {
                //benchmark Ordinary Radix Trie
                Stopwatch sw = Stopwatch.StartNew();
                for (int loop = 0; loop < rounds; loop++)
                {
                    long prefixCount = 0;
                    results.Clear();
                    suggestionRadixtrie.FindAllChildTerms(queryString.Substring(0, i + 1), 10, ref prefixCount, "", results, false);
                }
                sw.Stop();
                long time1 = sw.ElapsedMilliseconds;
                Console.WriteLine("ordinary search " + queryString.Substring(0, i + 1) + " in " + ((double)time1 / (double)rounds).ToString("N6") + " ms");
                //foreach ((string,long) result in results) Console.WriteLine(result.Item1+" "+result.Item2.ToString("N0"));


                //benchmark Pruning Radix Trie
                sw = Stopwatch.StartNew();
                for (int loop = 0; loop < rounds; loop++)
                {
                    long prefixCount = 0;
                    results.Clear();
                    suggestionRadixtrie.FindAllChildTerms(queryString.Substring(0, i + 1), 10, ref prefixCount, "", results, true);
                }
                sw.Stop();
                long time2 = sw.ElapsedMilliseconds;
                Console.WriteLine("pruning search " + queryString.Substring(0, i + 1) + " in " + ((double)time2 / (double)rounds).ToString("N6") + " ms");
                //foreach ((string,long) result in results) Console.WriteLine(result.Item1+" "+result.Item2.ToString("N0"));

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
