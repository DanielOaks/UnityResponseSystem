/*
 * DanielOaks' ResponseSystem library for Unity (https://github.com/DanielOaks/UnityResponseSystem)
 * ResponseSystem code distributed under CC0 Public Domain.
 * With MIT-licensed components.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DanielOaks.RS
{

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

        public RSFactDictionary Facts = new RSFactDictionary();

        // Start is called before the first frame update
        void Start()
        {
            // populate our base facts
            this.InitFacts();
        }

        public virtual void InitFacts() {
            // do nothing
        }

        public virtual void UpdateFacts() {
            // do nothing
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }

}
