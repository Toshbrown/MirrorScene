using UnityEngine;
using System.Collections;
using Exploder;

public class fallingThingColoider : MonoBehaviour {


    public ExploderObject exploder;
    private FRController controller;
    void Start() 
    {
        exploder = GameObject.Find("Exploder").GetComponent<ExploderObject>();
        controller = GameObject.Find("Controller").GetComponent<FRController>();
    }

    public void OnTriggerEnter(Collider otherObj)
    {

        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        if (otherObj.tag == "BodyPart")
        {
            exploder.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.06f, gameObject.transform.position.z);
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.06f, gameObject.transform.position.z);
            exploder.ExplodeObject(gameObject, null);
            controller.rockDestroyed();
            //Destroy(gameObject, 5);
        }

        if (otherObj.tag == "Ship")
        {
            exploder.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.06f, gameObject.transform.position.z);
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.06f, gameObject.transform.position.z);
            exploder.ExplodeObject(gameObject, null);
            //Destroy(gameObject, 5);
        }

    }

}
