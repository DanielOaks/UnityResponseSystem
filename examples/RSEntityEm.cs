using UnityEngine;
using DanielOaks.RS;

public class RSEntityEm : RSEntity
{
    GameObject playerGO;

    public override void InitFacts() {
        foreach (GameObject entityGO in GameObject.FindGameObjectsWithTag("ResponseSystemEntity")) {
            RSEntity entity = entityGO.GetComponent(typeof(RSEntity)) as RSEntity;
            if (entity.Name == "player") {
                this.playerGO = entityGO;
            }
        }
        this.Facts.Set("distanceToPlayer", this.DistanceFromEmToPlayer());
    }

    public override void UpdateFacts() {
        this.Facts.Set("distanceToPlayer", this.DistanceFromEmToPlayer());
    }

    float DistanceFromEmToPlayer() {
        return Vector2.Distance(gameObject.transform.position, this.playerGO.transform.position);
    }

    public override void DispatchResponse(RSResponse response, ref RSQuery query) {
        switch (response.ResponseType) {
            case RSResponseType.Say:
                Debug.Log("saying "+response.ResponseValue);
                break;
            case RSResponseType.Log:
                Debug.Log(response.ResponseValue);
                break;
            default:
                Debug.Log(response.ResponseType + " - " + response.ResponseValue);
                break;
        }
    }
}
