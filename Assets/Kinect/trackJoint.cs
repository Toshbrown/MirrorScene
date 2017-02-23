using UnityEngine;
using System.Collections;
using Windows.Kinect;


public class trackJoint : MonoBehaviour
{

    private GameObject KinectAndScreen;

    KinectBodyManager KBM;
    public bool realPosition = false;
    public bool reflectedPosition = false;
    public bool mirrorSpacePosition = false;
    public bool orthogonalMirrorSpace = false;


    public string interactionStyle
    {
        
        set
        {
            realPosition = false;
            reflectedPosition = false;
            mirrorSpacePosition = false; //the default
            orthogonalMirrorSpace = false;
            MeshRenderer render = this.GetComponent<MeshRenderer>();

            if (value == "realPosition")
            {
                realPosition = true;
                render.enabled = true;
            }
            else if (value == "reflectedPosition")
            {
                reflectedPosition = true;
                render.enabled = true;
            }
            else if (value == "mirrorSpacePosition")
            {
                mirrorSpacePosition = true;
                render.enabled = false;
            }
            else if (value == "orthogonalMirrorSpace")
            {
                orthogonalMirrorSpace = true;
                render.enabled = true;
            }
            else
            {
                mirrorSpacePosition = true;
                render.enabled = false;
            }

        }
    }

    public JointType joint = JointType.Head;
    public bool applyHeadRotation = false;

    //informatioal output 
    public Vector3 possition;
    public Quaternion rotation;
    public Vector3 correctedPos;
    public Quaternion correctedRot;

    Vector3 fixedOffSet = new Vector3();

    public bool handClosed = false;

    private Vector3 origScale;

    // Use this for initialization
    void Start()
    {
        KinectAndScreen = GameObject.Find("KinectAndScreen");
        KBM = KinectAndScreen.GetComponent<KinectBodyManager>();
        origScale = gameObject.transform.localScale;
        correctedPos = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Body body = KBM.GetClosestData();
        if (body != null)
        {

            //Possition 
            if (realPosition)
            {
                correctedPos = KBM.kinectPossitionTransforms(body, joint);
            }

            if (reflectedPosition)
            {
                correctedPos = KBM.reflectionTransform(body, joint);
            }

            if (orthogonalMirrorSpace)
            {
                correctedPos = KBM.orthogonalSpaceTransform(body, joint);
            }

            if (mirrorSpacePosition)
            {
                correctedPos = KBM.mirrorSpaceTransform(body, joint);
            }

            correctedPos = correctedPos + fixedOffSet;

            gameObject.transform.position = correctedPos;

            if (joint == JointType.Head && applyHeadRotation)
            {
                correctedRot = KBM.GetClosestFaceRotation();
                gameObject.transform.rotation = correctedRot;
            }

            //If its a hand is it open or closed?
            if (joint == JointType.HandRight && body.HandRightState == HandState.Closed)
            {
                handClosed = true;
            }
            else if (joint == JointType.HandLeft && body.HandLeftState == HandState.Closed)
            {
                handClosed = true;
            }
            else
            {
                handClosed = false;
            }
        }
    }
}