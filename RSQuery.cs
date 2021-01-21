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

        public void Set(string key, string value) {
            this.Facts[key] = Convert.ToSingle(MurmurHash2.Hash(value));
            this.RawFactStrings[key] = value;
        }

        public void Set(string key, int value) {
            this.Facts[key] = (float) value;
        }

        public void Set(string key, float value) {
            this.Facts[key] = value;
        }
    }

    public class RSQuery
    {
        RSFactDictionary facts = new RSFactDictionary();
        List<RSFactDictionary> extraFactDictionaries = new List<RSFactDictionary>();

        public void Set(string key, string value) {
            this.facts.Set(key, value);
        }

        public void Set(string key, int value) {
            this.facts.Set(key, value);
        }

        public void Set(string key, float value) {
            this.facts.Set(key, value);
        }

        public void AddFactDictionary(ref RSFactDictionary fd) {
            this.extraFactDictionaries.Add(fd);
        }

        public bool Matches(RSCriterion critereon) {
            if (this.facts.Facts.ContainsKey(critereon.Matchkey)) {
                return critereon.Matches(this.facts.Facts[critereon.Matchkey]);
            }
            foreach (var facts in this.extraFactDictionaries) {
                if (facts.Facts.ContainsKey(critereon.Matchkey)) {
                    return critereon.Matches(facts.Facts[critereon.Matchkey]);
                }
            }
            return false;
        }
    }

}
