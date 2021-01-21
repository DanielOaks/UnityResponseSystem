/*
 * DanielOaks' ResponseSystem library for Unity (https://github.com/DanielOaks/UnityResponseSystem)
 * ResponseSystem code distributed under CC0 Public Domain.
 * With MIT-licensed components.
 */

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System;
using UnityEngine;
using NReco.Csv;

namespace DanielOaks.RS
{

    [System.Serializable]
    public class RSBucketKey {
        public string name;
        [Tooltip("If true, then a rule with this key empty will be available everywhere.")]
        public bool emptyMeansAll;
    }

    public class RSManager : MonoBehaviour
    {
        [Tooltip("The subfolders of this folder will be read to find our ResponseSystem CSV configuration files. In other words, the paths we check will be /Assets/<this>/*/*.csv")]
        // [FolderPath] // really, you can't do something like this by default? unity please.
        public string rootRSFolder = "Responses";

        [Tooltip("Within how many seconds of the real time can the manager fire idle events? Lets the manager wait more efficiently.")]
        public float idleMungeSeconds = 0.1F;

        //TODO(dan): add world fact dictionaries here. we'll need to make a
        // Serializable class for this because Unity doesn't like exposing
        // dictionaries for in-editor editing. see how RSBucketKey does it.w

        [Tooltip("We split up our rules into separate buckets based on these keys.")]
        public List<RSBucketKey> bucketKeys = new List<RSBucketKey>();

        Dictionary<string,int> conceptIDs = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string,RSCriterion> criteria = new Dictionary<string,RSCriterion>();
        Dictionary<string,RSResponseGroup> responseGroups = new Dictionary<string,RSResponseGroup>();
        Dictionary<GameObject,DateTime> entityNextIdleTime = new Dictionary<GameObject,DateTime>();

        RSRulesBucket lazyAllRules = new RSRulesBucket();

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
                    this.AddCriterion(name, new RSCriterion(matchkey, matchvalue, weight, optional));
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
                        if (!(columnIDs.ContainsKey("name") && columnIDs.ContainsKey("criteria") && columnIDs.ContainsKey("responses") && columnIDs.ContainsKey("norepeat"))) {
                            throw new Exception("The Rules CSV file [" + path + "] does not contain all the columns we require");
                        }
                        continue;
                    }
                    // load in rule
                    string name = csvReader[columnIDs["name"]];
                    string criteria = csvReader[columnIDs["criteria"]];
                    string responses = csvReader[columnIDs["responses"]];
                    bool norepeat = false;
                    string norepeatString = csvReader[columnIDs["norepeat"]];
                    if (norepeatString != "") {
                        norepeat = Convert.ToBoolean(norepeatString);
                    }

                    if (name == "") {
                        // skip empty rows
                        continue;
                    }
                    var rule = new RSRule(name, criteria, responses, norepeat);
                    this.lazyAllRules.Insert(rule);
                }
            }
            Debug.Log(this.lazyAllRules);
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

        public void Run(ref RSQuery query, GameObject gameObject) {
            this.lazyAllRules.Run(ref query, this, gameObject);
        }

        public bool RunResponses(List<string> responses, ref RSQuery query, GameObject gameObject) {
            bool responseRun = false;
            foreach (var response in responses) {
                if (!this.responseGroups.ContainsKey(response)) {
                    continue;
                }
                var trr = this.responseGroups[response].Run(this, ref query, gameObject);
                if (responseRun == false) {
                    responseRun = trr;
                }
            }
            return responseRun;
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
                    Debug.Log("Found idling entity " + entity.Name);
                    var idleTime = DateTime.Now.AddSeconds(entity.secondsBetweenIdle + UnityEngine.Random.Range(entity.idleJitter*-1,entity.idleJitter));
                    this.entityNextIdleTime.Add(entityGO, idleTime);
                }
            }
            // spin off idle coroutine
            StartCoroutine("IdleLoop");

            // send off a fake idle event.
            var query = new RSQuery();
            query.Add("concept", "idle");
            query.Add("who", "em");
            // query.Add("action", "repairing");
            GameObject gameObjectEm = null;
            foreach (GameObject entityGO in GameObject.FindGameObjectsWithTag("ResponseSystemEntity")) {
                RSEntity entity = entityGO.GetComponent(typeof(RSEntity)) as RSEntity;
                if (entity.Name == "em") {
                    gameObjectEm = entityGO;
                    break;
                }
            }
            if (gameObjectEm != null) {
                Debug.Log("dispatching fake idle event for idling em");
                this.Run(ref query, gameObjectEm);
            }
        }

        IEnumerator IdleLoop()
        {
            GameObject nextIdlingEntity = null;
            while (true) {
                if (nextIdlingEntity != null) {
                    RSEntity entity = nextIdlingEntity.GetComponent(typeof(RSEntity)) as RSEntity;
                    // update idle time
                    var nextIdleTime = DateTime.Now.AddSeconds(entity.secondsBetweenIdle + UnityEngine.Random.Range(entity.idleJitter*-1,entity.idleJitter));
                    this.entityNextIdleTime[nextIdlingEntity] = nextIdleTime;

                    // actually make the entity idle
                    Debug.Log("entity "+entity.Name+" is idling");
                }
                // find next idling entity
                DateTime idleTime = DateTime.MaxValue;
                foreach (KeyValuePair<GameObject,DateTime> val in this.entityNextIdleTime) {
                    if (val.Value.CompareTo(idleTime) < 0) {
                        idleTime = val.Value;
                        nextIdlingEntity = val.Key;
                    }
                }
                float idleSecs = (float) idleTime.Subtract(DateTime.Now).TotalSeconds;
                if (this.idleMungeSeconds < idleSecs) {
                    yield return new WaitForSeconds(idleSecs);
                } else {
                    yield return null;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void OnDestroy()
        {
            StopCoroutine("IdleLoop");
        }
    }

}
