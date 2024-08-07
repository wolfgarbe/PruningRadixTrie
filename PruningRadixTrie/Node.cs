﻿using System.Collections.Generic;

namespace PruningRadixTrie
{
    //Trie node class
    public class Node
    {
        public (string key, Node node)[] Children;

        //Does this node represent the last character in a word? 
        //0: no word; >0: is word (termFrequencyCount)
        public long termFrequencyCount;
        public long termFrequencyCountChildMax;

        public Node(long termfrequencyCount)
        {
            termFrequencyCount = termfrequencyCount;
        }
    }
}
