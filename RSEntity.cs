using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RSEntity : MonoBehaviour
{
    [Tooltip("Name that the response system refers to this with. If this is a common entity (can, door, etc) then use the same name for all of these entities.")]
    public string Name;

    [Header("Idle Actions")]

    [Tooltip("Does this entity launch its own idle actions? e.g. NPCs can idle, but a can lying on the ground cannot.")]
    public bool canIdle;

    [Tooltip("How many seconds between each idle action on this character.")]
    [Min(1)]
    public float secondsBetweenIdle = 6;
    [Tooltip("Idle actions can happen this many seconds before/after the above time, randomly. Introduces more variety to when the idle actions occur.")]
    [Min(0)]
    public float idleJitter = 1;

    //TODO(dan): add entity fact dictionaries here. we'll need to make a
    // Serializable class for this because Unity doesn't like exposing
    // dictionaries for in-editor editing. see how RSBucketKey does it.

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
