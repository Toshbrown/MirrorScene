using UnityEngine;
using System.Collections;

public class Range {
    float min;
    float max;
    public Range( float minimum, float maximum)
    {
        min = minimum;
        max = maximum;
    }

    public float clampToRange(float val) {
        if(val > max) {
            return max;
        }
        if(val < min) {
            return min;
        }
        return val;
    }

    public bool isInRange(float val) {
        if(val <= max && val >= min) {
            return true;
        }
        return false;
    }
}

public class dragableObject : MonoBehaviour {

    public Color hoverColor = Color.red;

    private bool freezeX = false;
    private bool freezeY = false;
    private bool freezeZ = true;

    public float rangeXMin = -1000f;
    public float rangeXMax = 1000f;
    public float rangeYMin = -1000f;
    public float rangeYMax = 1000f;
    public float rangeZMin = -1000f;
    public float rangeZMax = 1000f;

    public Range rangeX;
    public Range rangeY;
    public Range rangeZ;

    public Vector3 debugPos = new Vector3();

    private Color _origColor;
    private bool _origColorLock = false;
    private bool handEnteredOpen = false;


    private Renderer rend;
    private Controller controller;
    void Start()
    {
        rend = gameObject.GetComponent<Renderer>();
        controller = GameObject.Find("Controller").GetComponent<Controller>();
        rangeX = new Range(rangeXMin, rangeXMax);
        rangeY = new Range(rangeYMin, rangeYMax);
        rangeZ = new Range(rangeZMin, rangeZMax);

    }

    public void Update()
    {

    }

    public float getPos()
    {
        return gameObject.transform.localPosition.x + 0.5f;
    }

    public bool setPos(float pos)
    {
        if(pos > 1.0f) {
            return false;
        }

        Vector3 newPos = new Vector3(gameObject.transform.localPosition.x - 0.5f, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z);
        gameObject.transform.localPosition = newPos;
        return true;
    }

    public void OnTriggerEnter(Collider otherObj)
    {

        if (otherObj.CompareTag("Mirror"))
        {
            return;
        }

        Debug.Log("Enter " + otherObj.ToString());

        var jointScript = otherObj.GetComponent<trackJoint>();
        if (jointScript != null && jointScript.handClosed == false)
        {
            if(!_origColorLock) {
                _origColor = rend.material.color;
            }
            rend.material.color = hoverColor;
            handEnteredOpen = true;
        }
        else
        {
            handEnteredOpen = false;
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
                //Vector3 newPos = gameObject.transform.parent.InverseTransformPoint(otherObj.transform.position);
                Vector3 newPos = otherObj.transform.position;
                if (!freezeX)
                {
                    newPos.x = rangeX.clampToRange(otherObj.transform.position.x);
                } 
                else
                {
                    newPos.x = gameObject.transform.localPosition.x;
                }
                
                if (!freezeY)
                {
                    newPos.y = rangeY.clampToRange(gameObject.transform.position.y);
                }
                else
                {
                    newPos.y = gameObject.transform.localPosition.y;
                }

                if (!freezeZ)
                {
                    newPos.z = rangeZ.clampToRange(gameObject.transform.position.z);
                }
                else
                {
                    newPos.z = gameObject.transform.localPosition.z;
                }

                debugPos = newPos;
                gameObject.transform.localPosition = newPos;
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

        rend.material.color = _origColor;
        _origColorLock = false;

    }
}
