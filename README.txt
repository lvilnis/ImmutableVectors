
 == ABOUT ==
 
 This library aims to provide an immutable data structure which offers fast put/get to arbitrary indices,
 and fast append/concat to either front or back (versions of the data structure offering fast both prepend/append,
 and cheap slicing, are in the works). It is very much a work in progress, but (modulo a small bug or two) 
 is already very fast and useful - the basic data structure is a nearly  line-for-line clone of Clojure's 
 PersistentVector data structure, but with several enhancements:
  
  - Typed leaf arrays (.NET-only, and led to a 4x speedup on some of my integer-based benchmarks - YMMV)
  - Both Prependable and Appendable versions, with conversion between the two when using the non-fast operations
  - Fast forward and backward enumerators
  - Fast copy from/to array (turning an array into a vector can be even faster than copying to another array for 
    1 million+ element arrays, due to the difficulty of allocating huge contiguous blocks of memory vs. 32-wide
	blocks)
  - A VectorProjection wrapper that provides lazy mapping, with deforesting optimizations such as map fusion and
    map/fold fusion
  - Specialized implementations of many commonly used utility functions and LINQ operators

 == KNOWN ISSUES ==
 
 - Extra array allocations: A lot of the map/filter/etc function implementations require an extra array allocation.
   This is because there is not yet any fast way of initializing a vector from a general List<T>, IList<T>,
   or IEnumerable<T>. This is quite simple (just take the existing T[] constructor and replace Array.Copy with
   List.CopyTo or Enumerator.MoveNext), and should be done soon.
 - "Reverse" enumeration bug: It appears that some vector (Prependable?) returns a reversed version of itself as
   a result of certain (tail?) operations. I haven't had time to look into this yet, but it should also be resolved 
   soon.
 - Implementations of Slice and SliceToArray are missing (just stubs).
 
 == TO DO / NOTES ==
 
 * WARNING * This to-do list is quite verbose, and uh, borders on stream-of-consciousness.
 
 - Fix the above "Known Issues". Very doable.
 - Add a real benchmark. Probably figure out whatever suite the Scala/Clojure teams use to benchmark their data
   structures, and copy it.
 - Add fast bulk append/prepend. At the moment concat is implemented as repeated appending, which does a 
   copy-on-write every time.
 - Make a version of the vector that supports more structural sharing: I would like arbitrary slices of vectors
   to be able to share data with the vectors they are "sliced" from. One way of accomplishing this is to have a 
   vector that supports fast prepend _and_ append - that way slicing can share all the middle blocks and only copy
   the ends. This may be overkill, though. Instead: Add an enumerator that provides the 32-wide data "blocks", 
   not the data themselves. Along with a constructor that takes these blocks and an extra left-tail to handle 
   mis-alignment (or a startindex integer), we could make vector copies share all leaf data and copy only internal
   nodes (unless I'm missing something). This is probably the most important because IMHO the big disadvantage of
   transient arrays/arraylists is memory hogging. We could go further and try to avoid internal node copies, but I
   think that might get weird because the "tail" would lose its special status and appends would become updates...
 - Clean the code up: There is tons of copy-pasted code, much of it with slight variations (the best kind of 
   copy-paste...). Specifically, the PrependableVector class started as a wholesale copy of AppendableVector, 
   and the enumeration and construct-from-array code are full of depth-case manual inlining. I am not yet sure 
   of how much I can clean up some of the manual inlining without sacrificing performance, but at the very least,
   pulling each case out to a separate method should be easily inlined by the compiler if necessary. Additionally,
   many of the implementations of higher-order functions can be shared between vectors and should be pulled out to 
   extn methods, or at least made virtual in an abstract superclass to allow for overriding implementations in 
   cases such as VectorProjection's map fusion optimizations.
 - Figure out what to do with the VectorSlice class. In light of the many ideas about how to incorporate fast 
   slicing into the base data structure, the VectorSlice concept may soon become unnecessary.
 - Make a "deque" version: There are a few ways of going about this, I'd like to experiment with all of them:
  - Two copies of the vector (root + tail) data structure -  a "left" one and a "right" one. The right one works 
    like the normal vector and fills in the trie left-to-right, the left one is a reversed version and fills in 
    the trie right-to-left. New items are prepended to the left trie and appended to the right. If the vector is
    tail'd or pop'd enough times to empty either the left or right trie, we divide the still-full trie "in half" 
    (keep all leaf arrays and create all new internal nodes), and set the two halves as the new left and right 
    nodes.
  - Make an asymmetrical version with semi-fast prepend. We have the normal vector root+tail structure, but with 
    an extra "left tail" array. We fill the array in right-to-left, and once every 32 prepends we push the 
    "left tail" down into the data structure, and have to copy *all* of the non-leaf arrays. This is very similar
    to the shared-slicing idea above, and is probably just the continuation of that same line of reasoning. Related
    to the item below, I've looked at Scala's Vector a little and while I didn't catch everything that's going on,
    I think this is something like what they do.
  - Copy Scala's Vector data structure. I've looked at the code a bit and it seems to be really piggish with 
    memory for small vector sizes, due to the maintenance of 6 different "display" pointers, but it may be that
    the above ideas have fatal flaws and that the Scala implementation works better (they certainly know a lot more
    about data structures than I do!). I've only looked at it briefly and definitely didn't get everything that 
    was going on (I	have trouble reading excessive bit-operations), but I wouldn't be surprised if it actually uses 
    some of the above ideas. AFAIK we both started with Rich Hickey's 32-wide array-of-arrays and I might just reach
    some of the same conclusions they did.