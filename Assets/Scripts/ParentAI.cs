using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentAI : MonoBehaviour
{
    public GameObject player;
    public Camera viewport;
    public LayerMask blocksViewMask;
    
    public float timeToAlert;

    float seenTime;
    bool isPlayerOnScreen;
    bool isAlert;

    void Update() {
        Vector3 playerPositionOnScreen = viewport.WorldToViewportPoint(player.transform.position);
        isPlayerOnScreen = (playerPositionOnScreen.z > 0 && playerPositionOnScreen.x > 0 && playerPositionOnScreen.x < 1 && playerPositionOnScreen.y > 0 && playerPositionOnScreen.y < 1) && !Physics.Raycast(viewport.transform.position, player.transform.position - viewport.transform.position, (player.transform.position - viewport.transform.position).magnitude, blocksViewMask);
        
        if (isAlert) 
        {
            if (isPlayerOnScreen) {
                seenTime = timeToAlert;
            } else {
                seenTime -= Time.deltaTime;
                
                if (seenTime <= 0) {
                    isAlert = false;
                }
            }
        }
        else 
        {
            if (isPlayerOnScreen) {
                seenTime += Time.deltaTime;

                if (seenTime >= timeToAlert) {
                    isAlert = true;
                }

            } else {
                seenTime -= Time.deltaTime;
            }
        }
    }
}
