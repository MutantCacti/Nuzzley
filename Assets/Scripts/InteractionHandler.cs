using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    public void OnInteraction() 
    {
        transform.position += new Vector3(Random.Range(0f,20f), 0f, Random.Range(0f,20f));
        while (!Physics.Raycast(transform.position, Vector3.down)) {
            transform.position += new Vector3(Random.Range(0f,20f), 0f, Random.Range(0f,20f));
        }
        transform.rotation = Quaternion.Euler(Random.Range(0f,90f), Random.Range(0f,90f), Random.Range(0f,90f));
    }
}
