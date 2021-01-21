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

        public bool Run(ref RSQuery query, GameObject gameObject) {
            //TODO(dan): implement last flags properly
            Debug.Log("Running ResponseGroup called "+this.Name);
            if (this.firstResponse != null) {
                RSResponse firstResponse = this.responses[(int) this.firstResponse];
                if (firstResponse.CanFire()) {
                    this.RunResponse(firstResponse);
                    return true;
                }
            }
            if (this.flags.HasFlag(RSResponseGroupFlags.Sequential)) {
                // run responses sequentially
                foreach (var response in this.responses) {
                    if (response.CanFire()) {
                        this.RunResponse(response);
                        return true;
                    }
                }
            } else {
                // get a random response, weighted appropriately, and fire it.
                float totalWeight = 0;
                RSResponse lastResponse = null;
                foreach (var response in this.responses) {
                    if (response.CanFire()) {
                        totalWeight += response.Weight;
                        lastResponse = response;
                    }
                }
                if (lastResponse == null) {
                    // no valid responses
                    return false;
                }
                float weight = UnityEngine.Random.Range(0F, totalWeight);
                foreach (var response in this.responses) {
                    if (response.CanFire()) {
                        weight -= response.Weight;
                    }
                    if (weight <= 0 || response == lastResponse) {
                        this.RunResponse(response);
                        return true;
                    }
                }
            }
            return false;
        }

        void RunResponse(RSResponse response) {
            Debug.Log("RunResp: "+response.ResponseValue);
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
        public RSResponseType ResponseType;
        public string ResponseValue;
        bool delayAuto;
        float delaySeconds;
        float odds;
        float resayDelaySeconds;
        DateTime dontResayBefore;
        public float Weight;
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

            this.ResponseValue = responseValue;
            switch (responseType) {
                case "log":
                    this.ResponseType = RSResponseType.Log;
                    break;
                case "say":
                    this.ResponseType = RSResponseType.Say;
                    break;
                default:
                    this.ResponseType = RSResponseType.Log;
                    this.ResponseValue = "Invalid response type ["+responseType+"] with value ["+responseValue+"]";
                    break;
            }

            this.delayAuto = delayAuto;
            this.delaySeconds = delaySeconds;
            this.odds = odds;
            this.resayDelaySeconds = resayDelaySeconds;
            this.dontResayBefore = DateTime.Now.AddSeconds(-10F);
            this.Weight = weight;
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
            return DateTime.Compare(DateTime.Now, this.dontResayBefore) >= 0 && !this.disabled;
        }
    }

}
