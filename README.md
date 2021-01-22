# ResponseSystem for Unity

This response system is designed after Valve's response system used in the Source engine, and described in their GDC talk [_"Rule Databases for Contextual Dialog and Game Logic"_](https://youtu.be/tAbBID3N64A).

Below, we describe our implementation of this system, which borrows fairly heavily from the above talk and [this page on the Valve Developer Wiki](https://developer.valvesoftware.com/wiki/Response_System) which breaks down how their system works.

There's a companion spreadsheet where you can lay out all your logic - concepts, criteria, rules, and responses. Then you export each of those sheets as separate csvs (named as below), throw them all in the same folder. Here's the Google Sheets spreadsheet, containing example data: [ResponseSystem data](https://docs.google.com/spreadsheets/d/1kosKhT2fEJZOOKDXI4D5GhtiRoo83q70Nqcz19FnEtA/edit?usp=sharing).

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

You should create a class based on `RSEntity` for any node that will idle, so that you can dynamically populate its fact dictionary (e.g. how far away from me is player right now, what state is the GameObject I'm associated with in?). You should do the same with `RSManager`, and populate the world information just the same. The **examples** folder contains some example entities and managers.

## License
The design and function of this code borrows heavily from other sources noted above (Valve's talk and the pages explaining how their implementation of this system works). I consider the ResponseSystem code and associated spreadsheet to be licensed under CC0 Public Domain, with a few MIT-licensed components. Attribution would be cool if you use this, but isn't required.
