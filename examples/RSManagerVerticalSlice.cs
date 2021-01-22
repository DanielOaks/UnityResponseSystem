using UnityEngine;
using DanielOaks.RS;

public class RSManagerVerticalSlice : RSManager
{
    public string MapName = "test";

    public override void InitFacts() {
        this.Facts.Set("map", this.MapName);
    }
}
