PruningRadixTrie<br> 
[![MIT License](https://img.shields.io/github/license/wolfgarbe/pruningradixtrie.png)](https://github.com/wolfgarbe/PruningRadixTrie/blob/master/LICENSE)
========
**PruningRadixTrie - a Radix trie on steroids**

The PruningRadixTrie is a novel data structure, derived from a radix trie - but 3 orders of magnitude faster.

A [Radix Trie](https://en.wikipedia.org/wiki/Radix_tree) or Patricia Trie is a space-optimized trie (prefix tree).<br>
A **Pruning Radix trie** is a novel Radix trie algorithm, that allows a pruning of the Radix trie and early termination of the lookup.

In many cases we are not interested in a complete set of all childs for a given prefix, but only in the top-k most relevant terms.
Especially for short prefixes this results in a **massive reduction of lookup time** for the top-10 results.
On the other hand, a complete result set of millions of suggestions wouldnt be helpful at all for autocompletion.
This is achieved by storing in each node the maximum rank of all its childs. By comparing this maximum child rank with the lowest rank of the results retrieved so far, 
we can easily prune the trie and do an early termination of the look up for non-promising branches with low child ranks.

### Application:

PruningRadixTrie can be used for auto completion, query completion

### Performance

Pruning  Radix Trie: search top-10 results for prefix 'a' in 6.273.234 terms in     51 ms 
Ordinary Radix Trie: search top-10 results for prefix 'a' in 6.273.234 terms in 37.226 ms

700x faster


### Operations: 

**AddTerm:** insert a term into the Pruning Radix Trie.

**GetTopkTermsForPrefix:** retieve the top-k most relevant terms for a given previx from the Pruning Radix Trie.

**WriteTermsToFile:** Serialise the Pruning Radix Trie to disk for persistence.

**ReadTermsFromFile:** Deserialise the Pruning Radix Trie from disk for persistence.
