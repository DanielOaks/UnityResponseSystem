/*
 * DanielOaks' ResponseSystem library for Unity (https://github.com/DanielOaks/UnityResponseSystem)
 * ResponseSystem code distributed under CC0 Public Domain.
 * With MIT-licensed components.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DanielOaks.RS
{

    public class RSFactDictionary
    {
        public Dictionary<string,float> Facts = new Dictionary<string,float>();
        public Dictionary<string,string> RawFactStrings = new Dictionary<string,string>();

        public void Add(string key, string value) {
            this.Facts.Add(key, Convert.ToSingle(MurmurHash2.Hash(value)));
            this.RawFactStrings.Add(key, value);
        }

        public void Add(string key, int value) {
            this.Facts.Add(key, (float) value);
        }

        public void Add(string key, float value) {
            this.Facts.Add(key, value);
        }
    }

    public class RSQuery
    {
        RSFactDictionary facts = new RSFactDictionary();
        List<RSFactDictionary> extraFactDictionaries = new List<RSFactDictionary>();

        public void Add(string key, string value) {
            this.facts.Add(key, value);
        }

        public void Add(string key, int value) {
            this.facts.Add(key, value);
        }

        public void Add(string key, float value) {
            this.facts.Add(key, value);
        }

        public void AddFactDictionary(ref RSFactDictionary fd) {
            this.extraFactDictionaries.Add(fd);
        }
    }

}
