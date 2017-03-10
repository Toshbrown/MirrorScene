using UnityEngine;
using System.Collections;
using Exploder;

public class fallingThingColoider : MonoBehaviour {


    public ExploderObject exploder;
    public TextMesh numberTextMesh;
    private string _number;
 
    void Start() 
    {
        exploder = GameObject.Find("Exploder").GetComponent<ExploderObject>();
        numberTextMesh = this.GetComponentInChildren<TextMesh>();

        if(_number != null)
        {
            numberTextMesh.text = _number;
        }
    }

    public void setNumber(int num)
    {
        _number = num.ToString();
        if (numberTextMesh != null)
        {
            numberTextMesh.text = _number;
        }
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
            if (canHit())
            {
                Destroy(numberTextMesh);
                exploder.ExplodeObject(gameObject.transform.Find("rock").gameObject, null);
            }
            //Destroy(gameObject, 5);
        }

    }

    private bool canHit()
    {
        if (numberTextMesh.text == "")
        {
            return true;
        }
        else if ( (IntParseFast(numberTextMesh.text) % 2) == 1 )
        {
                return true;
        }

        return false;
    }
    public void OnTriggerStay(Collider otherObj)
    {
        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        if (numberTextMesh != null)
        {
            Debug.Log("Hover:: numberTextMesh " + numberTextMesh.text);

            if (canHit())
            {
                var jointScript = otherObj.GetComponent<trackJoint>();
                if (jointScript != null && handEnteredOpen)
                {
                    if (jointScript.handClosed == true)
                    {
                        Destroy(numberTextMesh);
                        exploder.ExplodeObject(gameObject.transform.Find("rock").gameObject);
                    }
                }
            }
        }
    }

    

     public static int IntParseFast(string value)
     {
     int result = 0;
     for (int i = 0; i < value.Length; i++)
     {
         char letter = value[i];
         result = 10 * result + (letter - 48);
     }
     return result;
     }

}
