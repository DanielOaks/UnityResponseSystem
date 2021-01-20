/*
 * DanielOaks' ResponseSystem library for Unity (https://github.com/DanielOaks/UnityResponseSystem)
 * ResponseSystem code distributed under CC0 Public Domain.
 * With MIT-licensed components.
 */

using System.Collections.Generic;
using System;

namespace DanielOaks.RS
{

    public class RSRulesBucket {
        List<RSRule> rules = new List<RSRule>();

        // rules are ordered in our bucket from:
        //  most criteria -> least criteria
        // this is described in the responsesystem talk, and is because if a rule
        // with more criteria (that's more specific) matches, we don't need to find
        // a rule that's less specific, we've already got the most specific one \o/
        public void Insert(RSRule rule) {
            int c = rule.CriteriaCount();

            if (this.rules.Count == 0) {
                this.rules.Add(rule);
                return;
            }

            for (int i=0; i <= c; i++) {
                if (i == c) {
                    this.rules.Add(rule);
                    break;
                } else if (this.rules[i].CriteriaCount() < c) {
                    this.rules.Insert(i, rule);
                    break;
                }
            }
        }

        public override string ToString()
        {
            var sl = new List<string>();
            foreach (var rule in this.rules) {
                sl.Add(rule.Name+":"+rule.CriteriaCount().ToString());
            }
            string s = String.Join(",", sl);
            return $"({s})";
        }
    }

    [Flags]
    public enum RSRuleFlags
    {
        None = 0,
        NoRepeat = 1,
    }

    public class RSRule {
        public string Name;
        bool disabled; // automagically as a result of flags
        public RSRuleFlags Flags;
        List<string> criteria = new List<string>();
        List<string> responses = new List<string>();

        public RSRule(string name, string criteria, string responses, bool norepeat) {
            this.Name = name;
            char[] splitters = {' '};
            foreach (var criteriaName in criteria.Split(splitters, System.StringSplitOptions.RemoveEmptyEntries)) {
                this.criteria.Add(criteriaName);
            }
            foreach (var responseName in responses.Split(splitters, System.StringSplitOptions.RemoveEmptyEntries)) {
                this.responses.Add(responseName);
            }

            if (norepeat) {
                this.Flags |= RSRuleFlags.NoRepeat;
            }
        }

        public int CriteriaCount() {
            return this.criteria.Count;
        }

        // public List<string> BucketsThisGoesIn(List<RSBucketKey> bucketKeys) {

        // }
    }

}
