using UnityEngine;
using System.Collections;

public class lunarColider : MonoBehaviour {

    private FRController controller;

    void Start()
    {
        controller = GameObject.Find("Controller").GetComponent<FRController>();

    }

    public void OnTriggerEnter(Collider otherObj)
    {

        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        Debug.Log("Enter");

        if (otherObj.tag == "Exploder")
        {
            controller.removeLife();
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
        if (jointScript != null)
        {
            if (jointScript.handClosed == true)
            {
                //gameObject.transform.position = otherObj.transform.position;
            }
        }
    }

    public void OnTriggerExit(Collider otherObj)
    {
        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        Debug.Log("Exit");
    }

}
