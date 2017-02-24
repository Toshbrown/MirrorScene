using UnityEngine;
using System.Collections;
using Exploder;

public class fallingThingColoider : MonoBehaviour {


    public ExploderObject exploder;

    void Start() 
    {
        exploder = GameObject.Find("Exploder").GetComponent<ExploderObject>();
    }

    private bool handEnteredOpen;
    public void OnTriggerEnter(Collider otherObj)
    {

        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        var jointScript = otherObj.GetComponent<trackJoint>();
        if (jointScript != null && jointScript.handClosed == false)
        {
            handEnteredOpen = true;
        }
        else
        {
            handEnteredOpen = false;
        }

        if (otherObj.tag == "BodyPart")
        {
            //exploder.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.06f, gameObject.transform.position.z);
            //gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.06f, gameObject.transform.position.z);
            //exploder.ExplodeObject(gameObject, null);
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

    public void OnTriggerStay(Collider otherObj)
    {
        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        Debug.Log("Hover");

        var jointScript = otherObj.GetComponent<trackJoint>();
        if (jointScript != null && handEnteredOpen)
        {
            if (jointScript.handClosed == true)
            {
                exploder.ExplodeObject(gameObject, null);
            }
        }
    }

}
