using UnityEngine;
using System.Collections;

public class TestCollision : MonoBehaviour {
    public void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject.name + "-OnTriggerEnter: " + other.gameObject.name);
    }

    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log(gameObject.name + "-OnCollisionEnter: " + collision.gameObject.name);
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


}
