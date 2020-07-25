using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace PruningRadixTrie
{
    /// <summary>
    /// Summary description for Trie
    /// </summary>
    public partial class PruningRadixTrie
    {
        public long termCount = 0;
        public long termCountLoaded = 0;

        //The trie
        private readonly Node trie;

        public PruningRadixTrie()
        {
            trie = new Node(0);
        }

        // Insert a word into the trie
        public void AddTerm(String term, long termFrequencyCount)
        {
            List<Node> nodeList = new List<Node>();
            AddTerm(trie, term, termFrequencyCount, 0, 0, nodeList);
        }

        public void UpdateMaxCounts(List<Node> nodeList, long termFrequencyCount)
        {
            foreach (Node node in nodeList) if (termFrequencyCount > node.termFrequencyCountChildMax) node.termFrequencyCountChildMax = termFrequencyCount;
        }

        public void AddTerm(Node curr, String term, long termFrequencyCount, int id, int level, List<Node> nodeList)
        {
            try
            {
                nodeList.Add(curr);

                //test for common prefix (with possibly different suffix)
                int common = 0;
                if (curr.Children != null)
                { 
                    for (int j = 0; j < curr.Children.Count; j++)
                    {
                        (string key, Node node) = curr.Children[j];

                        for (int i = 0; i < Math.Min(term.Length, key.Length); i++) if (term[i] == key[i]) common = i + 1; else break;

                        if (common > 0)
                        {
                            //term already existed
                            //existing ab
                            //new      ab
                            if ((common == term.Length) && (common == key.Length))
                            {
                                if (node.termFrequencyCount == 0) termCount++;
                                node.termFrequencyCount += termFrequencyCount;
                                UpdateMaxCounts(nodeList, node.termFrequencyCount);
                            }
                            //new is subkey
                            //existing abcd
                            //new      ab
                            //if new is shorter (== common), then node(count) and only 1. children add (clause2)
                            else if (common == term.Length)
                            {
                                //insert second part of oldKey as child 
                                Node child = new Node(termFrequencyCount);
                                child.Children = new List<(string, Node)>
                                {
                                   (key.Substring(common), node)
                                };
                                child.termFrequencyCountChildMax = Math.Max(node.termFrequencyCountChildMax, node.termFrequencyCount);
                                UpdateMaxCounts(nodeList, termFrequencyCount);

                                //insert first part as key, overwrite old node
                                curr.Children[j] = (term.Substring(0, common), child);
                                //sort children descending by termFrequencyCountChildMax to start lookup with most promising branch
                                curr.Children.Sort((x, y) => y.Item2.termFrequencyCountChildMax.CompareTo(x.Item2.termFrequencyCountChildMax));
                                //increment termcount by 1
                                termCount++;
                            }
                            //if oldkey shorter (==common), then recursive addTerm (clause1)
                            //existing: te
                            //new:      test
                            else if (common == key.Length)
                            {
                                AddTerm(node, term.Substring(common), termFrequencyCount, id, level + 1, nodeList);
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
                                     (key.Substring(common), node) ,
                                     (term.Substring(common), new Node(termFrequencyCount))
                                };
                                child.termFrequencyCountChildMax = Math.Max(node.termFrequencyCountChildMax, Math.Max(termFrequencyCount, node.termFrequencyCount));
                                UpdateMaxCounts(nodeList, termFrequencyCount);

                                //insert first part as key. overwrite old node
                                curr.Children[j] = (term.Substring(0, common), child);
                                //sort children descending by termFrequencyCountChildMax to start lookup with most promising branch
                                curr.Children.Sort((x, y) => y.Item2.termFrequencyCountChildMax.CompareTo(x.Item2.termFrequencyCountChildMax));
                                //increment termcount by 1 
                                termCount++;
                            }
                            return;
                        }
                    }
                }

                // initialize dictionary if first key is inserted 
                if (curr.Children == null)
                {
                    curr.Children = new List<(string, Node)>
                        {
                            ( term, new Node(termFrequencyCount) )
                        };
                }
                else
                {
                    curr.Children.Add((term, new Node(termFrequencyCount)));
                    //sort children descending by termFrequencyCountChildMax to start lookup with most promising branch
                    curr.Children.Sort((x, y) => y.Item2.termFrequencyCountChildMax.CompareTo(x.Item2.termFrequencyCountChildMax));
                }
                termCount++;
                UpdateMaxCounts(nodeList, termFrequencyCount);
            }
            catch (Exception e) { Console.WriteLine("exception: " + term + " " + e.Message); }
        }

        public void FindAllChildTerms(String prefix, int topK, ref long termFrequencyCountPrefix, string prefixString, List<(string term, long termFrequencyCount)> results, bool pruning)
        {
            FindAllChildTerms(prefix, trie, topK, ref termFrequencyCountPrefix, prefixString, results, null, pruning);
        }

        public void FindAllChildTerms(String prefix, Node curr, int topK, ref long termfrequencyCountPrefix, string prefixString, List<(string term, long termFrequencyCount)> results, System.IO.StreamWriter file, bool pruning)
        {
            try
            {
                //pruning/early termination in radix trie lookup
                if (pruning && (topK > 0) && (results.Count == topK) && (curr.termFrequencyCountChildMax <= results[topK - 1].termFrequencyCount)) return;

                //test for common prefix (with possibly different suffix)
                bool noPrefix = string.IsNullOrEmpty(prefix);

                if (curr.Children != null)
                {
                    foreach ((string key, Node node) in curr.Children)
                    {                     
                        //pruning/early termination in radix trie lookup
                        if (pruning && (topK > 0) && (results.Count == topK) && (node.termFrequencyCount <= results[topK - 1].termFrequencyCount) && (node.termFrequencyCountChildMax <= results[topK - 1].termFrequencyCount))
                        {
                            if (!noPrefix) break; else continue;
                        }                     

                        if (noPrefix || key.StartsWith(prefix))
                        {
                            if (node.termFrequencyCount > 0)
                            {
                                if (prefix == key) termfrequencyCountPrefix = node.termFrequencyCount;

                                //candidate                              
                                if (file != null) file.WriteLine(prefixString + key + "\t" + node.termFrequencyCount.ToString());
                                else
                                if (topK > 0) AddTopKSuggestion(prefixString + key, node.termFrequencyCount, topK, ref results); else results.Add((prefixString + key, node.termFrequencyCount));                               
                            }

                            if ((node.Children != null) && (node.Children.Count > 0)) FindAllChildTerms("", node, topK, ref termfrequencyCountPrefix, prefixString + key, results, file, pruning);
                            if (!noPrefix) break;
                        }
                        else if (prefix.StartsWith(key))
                        {

                            if ((node.Children != null) && (node.Children.Count > 0)) FindAllChildTerms(prefix.Substring(key.Length), node, topK, ref termfrequencyCountPrefix, prefixString + key, results, file, pruning);
                            break;
                        }
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("exception: " + prefix + " " + e.Message); }
        }

        public List<(string term, long termFrequencyCount)> GetTopkTermsForPrefix(String prefix, int topK, out long termFrequencyCountPrefix, bool pruning=true)
        {
            List<(string term, long termFrequencyCount)> results = new List<(string term, long termFrequencyCount)>();

            //termFrequency of prefix, if it exists in the dictionary (even if not returned in the topK results due to low termFrequency)
            termFrequencyCountPrefix = 0;

            // At the end of the prefix, find all child words
            FindAllChildTerms(prefix, topK, ref termFrequencyCountPrefix, "", results,pruning);

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

        public class BinarySearchComparer : IComparer<(string term, long termFrequencyCount)>
        {
            public int Compare((string term, long termFrequencyCount) f1, (string term, long termFrequencyCount) f2)
            {
                return Comparer<long>.Default.Compare(f2.termFrequencyCount, f1.termFrequencyCount);//descending
            }
        }

        public void AddTopKSuggestion(string term, long termFrequencyCount, int topK, ref List<(string term, long termFrequencyCount)> results)
        {
            //at the end/highest index is the lowest value
            // >  : old take precedence for equal rank   
            // >= : new take precedence for equal rank 
            if ((results.Count < topK) || (termFrequencyCount >= results[topK - 1].termFrequencyCount))
            {
                int index = results.BinarySearch((term, termFrequencyCount), new BinarySearchComparer());
                if (index < 0) results.Insert(~index, (term, termFrequencyCount)); else results.Insert(index, (term, termFrequencyCount));

                if (results.Count > topK) results.RemoveAt(topK);
            }

        }

    }
}
