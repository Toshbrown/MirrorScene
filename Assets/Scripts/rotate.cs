using UnityEngine;
using System.Collections;

public class rotate : MonoBehaviour {

    public float speed = 0.0f;
    public enum axisOfRotation { x, y, z };
    public axisOfRotation axis = 0;

    private bool dir = false;

    // Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        switch (axis)
        {
            case axisOfRotation.x:
                gameObject.transform.localEulerAngles = new Vector3(gameObject.transform.localEulerAngles.x + speed, gameObject.transform.localEulerAngles.y, gameObject.transform.localEulerAngles.z);
                break;
            case axisOfRotation.y:
                gameObject.transform.localEulerAngles = new Vector3(gameObject.transform.localEulerAngles.x, gameObject.transform.localEulerAngles.y + speed, gameObject.transform.localEulerAngles.z);
                break;
            case axisOfRotation.z:
                gameObject.transform.localEulerAngles = new Vector3(gameObject.transform.localEulerAngles.x, gameObject.transform.localEulerAngles.y, gameObject.transform.localEulerAngles.z + speed);
                break;
        }


        if (gameObject.transform.position.x > 0.1f )
        {
            dir = true;
        }
        if (gameObject.transform.position.x < -0.1f )
        {
            dir = false;
        }

        var yoffset = Mathf.Sin(Time.time) * 0.0003f; 

        if (dir)
        {
            gameObject.transform.position = new Vector3(gameObject.transform.position.x - Time.deltaTime * 0.05f, gameObject.transform.position.y + yoffset, gameObject.transform.position.z);
        }
        else
        {
            gameObject.transform.position = new Vector3(gameObject.transform.position.x + Time.deltaTime * 0.05f, gameObject.transform.position.y + yoffset, gameObject.transform.position.z);
        }
	}
}
