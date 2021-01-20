/*
 * DanielOaks' ResponseSystem library for Unity (https://github.com/DanielOaks/UnityResponseSystem)
 * ResponseSystem code distributed under CC0 Public Domain.
 * With MIT-licensed components.
 */

using System.Collections.Generic;
using System;
using UnityEngine;

namespace DanielOaks.RS
{

    [Flags]
    public enum RSResponseGroupFlags
    {
        None = 0,
        PermitRepeats = 1,
        Sequential = 2,
        NoRepeat = 4,
    }

    public class RSResponseGroup {
        public string Name;
        bool disabled; // automagically as a result of flags
        RSResponseGroupFlags flags;
        List<RSResponse> responses = new List<RSResponse>();
        int? firstResponse = null;
        int? lastResponse = null;

        public RSResponseGroup(string name, string flags) {
            this.Name = name;
            char[] splitters = {' '};
            foreach (var flagName in flags.Split(splitters, System.StringSplitOptions.RemoveEmptyEntries)) {
                switch (flagName) {
                    case "permitrepeats":
                        this.flags |= RSResponseGroupFlags.PermitRepeats;
                        break;
                    case "sequential":
                        this.flags |= RSResponseGroupFlags.Sequential;
                        break;
                    case "norepeat":
                        this.flags |= RSResponseGroupFlags.NoRepeat;
                        break;
                    default:
                        // we don't know this flag
                        break;
                }
            }
        }

        public RSResponseGroup(string name, RSResponseGroupFlags flags) {
            this.Name = name;
            this.flags = flags;
        }

        public void Run(ref RSQuery query, GameObject gameObject) {
            //TODO(dan): implement first and last flags properly \o/
            Debug.Log("Running ResponseGroup called "+this.Name);
        }

        public void Add(RSResponse response) {
            this.responses.Add(response);

            if (response.Flags.HasFlag(RSResponseFlags.First)) {
                this.firstResponse = this.responses.Count - 1;
            } else if (response.Flags.HasFlag(RSResponseFlags.Last)) {
                this.lastResponse = this.responses.Count - 1;
            }
        }
    }

    [Flags]
    public enum RSResponseFlags
    {
        None = 0,
        NoRepeat = 1,
        First = 2,
        Last = 3,
    }

    public enum RSResponseType
    {
        Log,
        Say,
    }

    public class RSResponse {
        bool disabled; // automagically as a result of flags
        public RSResponseFlags Flags;
        RSResponseType responseType;
        string responseValue;
        bool delayAuto;
        float delaySeconds;
        float odds;
        float resayDelaySeconds;
        DateTime dontResayBefore;
        float weight;
        //TODO(dan): add `then` response firing

        public RSResponse(string flags, string responseType, string responseValue, bool delayAuto, float delaySeconds, float odds, float resayDelaySeconds, float weight) {
            char[] splitters = {' '};
            foreach (var flagName in flags.Split(splitters, System.StringSplitOptions.RemoveEmptyEntries)) {
                switch (flagName) {
                    case "norepeat":
                        this.Flags |= RSResponseFlags.NoRepeat;
                        break;
                    case "first":
                        this.Flags |= RSResponseFlags.First;
                        break;
                    case "last":
                        this.Flags |= RSResponseFlags.Last;
                        break;
                    default:
                        // we don't know this flag
                        break;
                }
            }

            this.responseValue = responseValue;
            switch (responseType) {
                case "log":
                    this.responseType = RSResponseType.Log;
                    break;
                case "say":
                    this.responseType = RSResponseType.Say;
                    break;
                default:
                    this.responseType = RSResponseType.Log;
                    this.responseValue = "Invalid response type ["+responseType+"] with value ["+responseValue+"]";
                    break;
            }

            this.delayAuto = delayAuto;
            this.delaySeconds = delaySeconds;
            this.odds = odds;
            this.resayDelaySeconds = resayDelaySeconds;
            this.weight = weight;
        }

        // mark this respone as just being fired.
        public void JustFired() {
            if (0 < this.resayDelaySeconds) {
                this.dontResayBefore = DateTime.Now.AddSeconds(this.resayDelaySeconds);
            }
            if (this.Flags.HasFlag(RSResponseFlags.NoRepeat)) {
                this.disabled = true;
            }
        }

        public bool CanFire() {
            return DateTime.Compare(DateTime.Now, this.dontResayBefore) <= 0 && !this.disabled;
        }
    }

}
