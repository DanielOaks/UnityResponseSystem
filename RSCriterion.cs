/*
 * DanielOaks' ResponseSystem library for Unity (https://github.com/DanielOaks/UnityResponseSystem)
 * ResponseSystem code distributed under CC0 Public Domain.
 * With MIT-licensed components.
 */

using System;

namespace DanielOaks.RS
{

    public class RSCriterion {
        string matchkey;
        string matchvalue;
        float matchmin = Single.NegativeInfinity;
        float matchmax = Single.PositiveInfinity;
        float weight = 1;
        bool optional;

        public RSCriterion(string matchkey, string matchvalue, float weight, bool optional) {
            this.matchkey =  matchkey;
            this.matchvalue = matchvalue;
            this.weight = weight;
            this.optional = optional;

            //TODO(dan): parse out other matchvalue strings
            if (matchvalue.StartsWith("\"") && matchvalue.EndsWith("\"")) {
                this.matchmin = Convert.ToSingle(MurmurHash2.Hash(matchvalue.Substring(1, matchvalue.Length-2)));
                this.matchmax = this.matchmin;
            } else {
                throw new Exception("Could not parse matchvalue ["+matchvalue+"] from criterion ["+matchkey+"]");
            }
        }

        public bool Matches(string value) {
            // do we need to do weird epsilon stuff or whatever here??
            // see https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
            // and ~ slide 118 of the presentation.
            float realValue = Convert.ToSingle(MurmurHash2.Hash(value));
            return this.matchmin <= realValue && realValue <= this.matchmax;
        }

        public override string ToString()
        {
            return $"({matchkey},{matchvalue},{weight},{optional})";
        }
    }

}
