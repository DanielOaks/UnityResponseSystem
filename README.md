# ResponseSystem for Unity

This response system is designed after Valve's response system used in the Source engine, and described in their GDC talk [_"Rule Databases for Contextual Dialog and Game Logic"_](https://youtu.be/tAbBID3N64A).

Below, we describe our implementation of this system, which borrows fairly heavily from the above talk and [this page on the Valve Developer Wiki](https://developer.valvesoftware.com/wiki/Response_System) which breaks down how their system works.

There's a companion spreadsheet where you can lay out all your logic - concepts, criteria, rules, and responses. Then you export each of those sheets as separate csvs (named as below), throw them all in the same folder.

Your project layout should end up something like:

```
/<root unity project>
 assets/
        Responses/
                  Core/ <can be named anything>
                       concepts.csv
                       criteria.csv
                       responses.csv
                       rules.csv
        Scripts/
                ResponseSystem/
                               *.cs, contents of this repo, etc.
```

Apply the `RSEntity.cs` component to your nodes (NPCs, elements that NPCs will interact with, the player), I like doing this to an empty parented to the node so I can place the resulting text mox more easily. Create one empty node in your scene with the `RSManager.cs` component. Some good default bucket keys on the manager are `concept` (Empty Means All unchecked) and `map` (Empty Means All checked).

I dunno what I can licence this code given how heavily the system borrows from other sources, but attribution would be cool if you use this implementation. I consider this code and the associated spreadsheet CC0 Public Domain. This code includes several components which are MIT licensed.
