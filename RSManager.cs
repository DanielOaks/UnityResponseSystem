using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System;
using UnityEngine;
using NReco.Csv;

[System.Serializable]
public class RSBucketKey {
    public string name;
    [Tooltip("If true, then a rule with this key empty will be available everywhere.")]
    public bool emptyMeansAll;
}

public class RSCriterion {
    string matchkey;
    string matchvalue;
    float weight = 1;
    bool optional;

    public RSCriterion(string matchkey, string matchvalue, float weight, bool optional) {
        this.matchkey =  matchkey;
        this.matchvalue = matchvalue;
        this.weight = weight;
        this.optional = optional;
    }

    public bool Matches(string value) {
        //TODO(dan): Hash and use min/max values as the talk recommends lol.
        // this will probably involve splitting up matchvalue into min+max
        // values, which can be really easily compared. then, ALWAYS a<=x<=b (slide 118).
        return value == this.matchvalue;
    }

    public override string ToString()
    {
        return $"({matchkey},{matchvalue},{weight},{optional})";
    }
}

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

public class RSManager : MonoBehaviour
{
    [Tooltip("The subfolders of this folder will be read to find our ResponseSystem CSV configuration files. In other words, the paths we check will be /Assets/<this>/*/*.csv")]
    // [FolderPath] // really, you can't do something like this by default? unity please.
    public string rootRSFolder = "Responses";

    [Tooltip("Within how many seconds of the real time can the manager fire idle events? Lets the manager wait more efficiently.")]
    public float idleMungeSeconds = 0.1F;

    [Tooltip("We split up our rules into separate buckets based on these keys.")]
    public List<RSBucketKey> bucketKeys = new List<RSBucketKey>();

    Dictionary<string,int> conceptIDs = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
    Dictionary<string,RSCriterion> criteria = new Dictionary<string,RSCriterion>();
    Dictionary<string,RSResponseGroup> responseGroups = new Dictionary<string,RSResponseGroup>();
    List<GameObject> entitesThatCanIdle = new List<GameObject>();

    void LoadConceptsCSV(string path)
    {
        Debug.Log("Loading Concepts CSV: " + path);
        Dictionary<string,int> columnIDs = new Dictionary<string,int>();
        using (var streamRdr = new StreamReader(path)) {
            var csvReader = new CsvReader(streamRdr, ",");
            while (csvReader.Read()) {
                // load column names
                if (columnIDs.Count == 0) {
                    for (int i=0; i<csvReader.FieldsCount; i++) {
                        columnIDs.Add(csvReader[i], i);
                    }

                    // check that file is valid. if not, we die.
                    if (!(columnIDs.ContainsKey("name") && columnIDs.ContainsKey("priority"))) {
                        throw new Exception("The Concepts CSV file [" + path + "] does not contain all the columns we require");
                    }
                    continue;
                }
                // load in concept
                string name = csvReader[columnIDs["name"]];
                // string priority = csvReader[columnIDs["priority"]]; //TODO(dan): store and use this priority

                if (name == "") {
                    // skip empty rows
                    continue;
                }

                // add new concept to id dict
                if (conceptIDs.ContainsKey(name)) {
                    throw new Exception("The Concepts CSV file [" + path + "] re-defines the concept [" + name + "] which is already defined");
                }
                int id = conceptIDs.Count;
                conceptIDs.Add(name, id);
            }
        }
    }

    void LoadCriteriaCSV(string path)
    {
        Debug.Log("Loading Criteria CSV: " + path);
        Dictionary<string,int> columnIDs = new Dictionary<string,int>();
        using (var streamRdr = new StreamReader(path)) {
            var csvReader = new CsvReader(streamRdr, ",");
            while (csvReader.Read()) {
                // load column names
                if (columnIDs.Count == 0) {
                    for (int i=0; i<csvReader.FieldsCount; i++) {
                        columnIDs.Add(csvReader[i], i);
                    }

                    // check that file is valid. if not, we die.
                    if (!(columnIDs.ContainsKey("name") && columnIDs.ContainsKey("matchkey") && columnIDs.ContainsKey("matchvalue") && columnIDs.ContainsKey("weight") && columnIDs.ContainsKey("optional"))) {
                        throw new Exception("The Criteria CSV file [" + path + "] does not contain all the columns we require");
                    }
                    continue;
                }
                // load in concept
                string name = csvReader[columnIDs["name"]];

                if (name == "") {
                    // skip empty rows
                    continue;
                }

                string matchkey = csvReader[columnIDs["matchkey"]];
                string matchvalue = csvReader[columnIDs["matchvalue"]];
                float weight = 1;
                if (csvReader[columnIDs["weight"]] != "") {
                    weight = float.Parse(csvReader[columnIDs["weight"]], CultureInfo.InvariantCulture.NumberFormat);
                }
                bool optional = false;
                if (csvReader[columnIDs["optional"]] != "") {
                    optional = Convert.ToBoolean(csvReader[columnIDs["optional"]]);
                }
                AddCriterion(name, new RSCriterion(matchkey, matchvalue, weight, optional));
            }
        }
    }

    void AddCriterion(string name, RSCriterion criterion) {
        if (this.criteria.ContainsKey(name)) {
            throw new Exception("The criterion [" + name + "] is already defined, cannot add a new criterion named this");
        }
        this.criteria.Add(name, criterion);
    }
    
    void LoadRulesCSV(string path)
    {
        Debug.Log("Loading Rules CSV: " + path);
    }
    
    void LoadResponsesCSV(string path)
    {
        Debug.Log("Loading Responses CSV: " + path);
        Dictionary<string,int> columnIDs = new Dictionary<string,int>();
        using (var streamRdr = new StreamReader(path)) {
            var csvReader = new CsvReader(streamRdr, ",");
            RSResponseGroup responseGroup = null;
            while (csvReader.Read()) {
                // load column names
                if (columnIDs.Count == 0) {
                    for (int i=0; i<csvReader.FieldsCount; i++) {
                        columnIDs.Add(csvReader[i], i);
                    }

                    // check that file is valid. if not, we die.
                    if (!(columnIDs.ContainsKey("name") && columnIDs.ContainsKey("flags") && columnIDs.ContainsKey("responsetype") && columnIDs.ContainsKey("response") && columnIDs.ContainsKey("delay") && columnIDs.ContainsKey("odds") && columnIDs.ContainsKey("resaydelay") && columnIDs.ContainsKey("weight"))) {
                        throw new Exception("The Responses CSV file [" + path + "] does not contain all the columns we require");
                    }
                    continue;
                }
                // load in response
                string name = csvReader[columnIDs["name"]];
                string flags = csvReader[columnIDs["flags"]];
                string responseTypeString = csvReader[columnIDs["responsetype"]];
                string responseValue = csvReader[columnIDs["response"]];

                if (name == "" && responseTypeString == "" && responseValue == "") {
                    // skip empty rows
                    continue;
                }

                string delayString = csvReader[columnIDs["delay"]];
                bool delayAuto = true;
                float delay = 0;
                if (delayString != "") {
                    delayAuto = false;
                    delay = float.Parse(delayString, CultureInfo.InvariantCulture.NumberFormat);
                }
                string oddsString = csvReader[columnIDs["odds"]];
                float odds = 1;
                if (oddsString != "") {
                    // we use 0-100 in the spreadsheet but 0-1 internally.
                    odds = float.Parse(delayString, CultureInfo.InvariantCulture.NumberFormat) / 100;
                }
                string resaydelayString = csvReader[columnIDs["resaydelay"]];
                float resaydelay = 0;
                if (resaydelayString != "") {
                    resaydelay = float.Parse(resaydelayString, CultureInfo.InvariantCulture.NumberFormat);
                }
                string weightString = csvReader[columnIDs["weight"]];
                float weight = 1;
                if (weightString != "") {
                    weight = float.Parse(weightString, CultureInfo.InvariantCulture.NumberFormat);
                }

                if (name != "") {
                    // add old responseGroup to our list of 'em
                    if (responseGroup != null) {
                        this.AddResponseGroup(responseGroup);
                    }

                    // create new responsegroup
                    responseGroup = new RSResponseGroup(name, flags);
                    // clear flags below so if this is a single-line response
                    //  group, these group flags aren't counted for *it* too.
                    flags = "";
                }
                if (responseTypeString != "" && responseValue != "") {
                    // add new response to responseGroup
                    responseGroup.Add(new RSResponse(flags, responseTypeString, responseValue, delayAuto, delay, odds, resaydelay, weight));
                }
            }
            if (responseGroup != null) {
                this.AddResponseGroup(responseGroup);
            }
        }
    }

    void AddResponseGroup(RSResponseGroup responseGroup) {
        if (this.responseGroups.ContainsKey(responseGroup.Name)) {
            throw new Exception("The responseGroup [" + responseGroup.Name + "] is already defined, cannot add a new responseGroup named this");
        }
        this.responseGroups.Add(responseGroup.Name, responseGroup);
    }

    // Start is called before the first frame update
    void Start()
    {
        // load all response system info from our csv files.
        //TODO(dan): this is probably very slow? we could create caches split
        // up by our bucket keys automagically, that will be regenerated
        // automagically if either our bucket keys change or the source csv
        // files change. Or something, idk.
        //TODO(dan): depending on how much our files expand, we could just skip
        // lines in the csv that don't apply to the current RSManager, given
        // that they're scene-specific. Eh, worry about that later when we
        // actually have enough content for it to affect us in a real way.
        DirectoryInfo rootdi = new DirectoryInfo(Path.Combine(Application.dataPath, rootRSFolder));
        foreach (var di in rootdi.EnumerateDirectories()) {
            foreach (var fi in di.EnumerateFiles()) {
                switch (fi.Name) {
                    case "concepts.csv":
                        this.LoadConceptsCSV(fi.FullName);
                        break;
                    case "criteria.csv":
                        this.LoadCriteriaCSV(fi.FullName);
                        break;
                    case "responses.csv":
                        this.LoadResponsesCSV(fi.FullName);
                        break;
                    case "rules.csv":
                        this.LoadRulesCSV(fi.FullName);
                        break;
                }
            }
        }

        // init all entities for idling.
        foreach (GameObject entityGO in GameObject.FindGameObjectsWithTag("ResponseSystemEntity")) {
            RSEntity entity = entityGO.GetComponent(typeof(RSEntity)) as RSEntity;
            if (entity.canIdle) {
                Debug.Log("Found idling entity " + entity.name);
                entitesThatCanIdle.Add(entityGO);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
