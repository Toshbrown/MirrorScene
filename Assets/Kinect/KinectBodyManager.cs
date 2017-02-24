using UnityEngine;
using System.Collections;
using Windows.Kinect;
using Microsoft.Kinect.Face;
using System.Collections.Generic;
using System.Linq;
public class KinectBodyManager : MonoBehaviour
{

    //Physical properties
    public GameObject MirrorPlane;

    //Editable Kinect properties
    public Vector3 kinectAxis = new Vector3(1, 0, 0); //which way is up
    public float kinectXoffset = -0.05f;
    public float kinectYoffset = 0.00f;

    //Caculated kinect properties
    public Vector3 _kinectPosition;
    public Quaternion _kinectRotation;

    //The Interaction Corridor in meters from the kinect sensor
    public float interactionCorridorLeftLimit = -0.25f;
    public float interactionCorridorRightLimit = 0.43f;
    public float interactionCorridorCloseLimit = 0;
    public float interactionCorridorFarLimit = 3.5f;
    //how far infrunt must a new user be to take controle of the interaction
    public float interactionCorridorZthreshold = 0.3f;
    public bool enforceInteractionCorridor = true;

    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;

    //standard face tracking for head angle
    public bool enableFacetracking = false;
    private FaceFrameSource faceFrameSource = null;
    private FaceFrameReader faceframeReader = null;
    //private FaceFrameFeatures currentfaceFrameFeatures;
    private UnityEngine.Quaternion currentFaceRotation;

    //Hd face tracking with mesh
    public bool enableHDFace = false;
    private HighDefinitionFaceFrameSource highDefinitionFaceFrameSource = null;
    private HighDefinitionFaceFrameReader highDefinitionFaceFrameReader = null;
    private FaceAlignment currentFaceAlignment = null;
    private FaceModel currentFaceModel;
    private FaceModel CurrentFaceModel
    {
        get
        {
            return currentFaceModel;
        }

        set
        {
            if (currentFaceModel != null)
            {
                currentFaceModel.Dispose();
                currentFaceModel = null;
            }

            currentFaceModel = value;
        }
    }

    private Vector3[] _CurrentFaceMeshVertices = null;
    private Vector3[] _CurrentFaceMeshCorrectedVertices = null;
    private int[] _CurrentFaceMeshTriangles = null;
    private Vector3 _CurrentFacePivot;
    private Vector3[] normals = null;
    private Vector2[] uvs = null;

    private Body[] _Data = null;
    private Body _Closest = null;
    private ulong _ClosestBodyId;
    public UnityEngine.Vector4 _FloorClipPlane;

    public LayerMask layerMask;

    private Controller controller;

    public Body[] GetData()
    {
        return _Data;
    }

    public Body GetClosestData()
    {
        return _Closest;
    }

    public Vector3 GetClosestFacePivot()
    {

        if (enableHDFace && _ClosestBodyId != 0)
        {
            return _CurrentFacePivot;
        }
        return new Vector3(0, 0, 0);
    }

    public Quaternion GetClosestFaceRotation()
    {
        if (enableFacetracking && faceFrameSource.TrackingId != 0)
        {
            return currentFaceRotation;
        }
        return new Quaternion();
    }
    public Vector3[] GetClosestFaceVertices()
    {

        if (enableHDFace && _ClosestBodyId != 0)
        {
            return _CurrentFaceMeshVertices;
        }
        return null;
    }

    public Vector3[] GetClosestFaceNormals()
    {

        if (enableHDFace && _ClosestBodyId != 0)
        {
            return normals;
        }
        return null;
    }

    public Vector2[] GetClosestFaceUVs()
    {

        if (enableHDFace && _ClosestBodyId != 0)
        {
            return uvs;
        }
        return null;
    }


    public Vector3[] GetClosestCorrectedFaceVertices()
    {

        if (enableHDFace && _ClosestBodyId != 0)
        {
            return _CurrentFaceMeshCorrectedVertices;
        }
        return null;
    }
    public int[] GetClosestFaceTriangles()
    {

        if (enableHDFace && _ClosestBodyId != 0)
        {
            return _CurrentFaceMeshTriangles;
        }
        return null;
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {

            _Reader = _Sensor.BodyFrameSource.OpenReader();

            if (enableFacetracking)
            {
                faceFrameSource = FaceFrameSource.Create(_Sensor, 0, FaceFrameFeatures.RotationOrientation);
                faceframeReader = faceFrameSource.OpenReader();
            }

            if (enableHDFace)
            {
                highDefinitionFaceFrameSource = HighDefinitionFaceFrameSource.Create(_Sensor);
                highDefinitionFaceFrameReader = highDefinitionFaceFrameSource.OpenReader();
                CurrentFaceModel = FaceModel.Create();
                currentFaceAlignment = FaceAlignment.Create();

                var triangles = new int[FaceModel.TriangleCount * 3];
                int tryCount = (int)FaceModel.TriangleCount;
                uint[] TriInd = FaceModel.TriangleIndices.ToArray();
                for (int i = 0; i < tryCount; i += 3)
                {
                    triangles[i] = (int)TriInd[i];
                    triangles[i + 1] = (int)TriInd[i + 1];
                    triangles[i + 2] = (int)TriInd[i + 2];

                }
                _CurrentFaceMeshTriangles = triangles;
            }

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

            controller = GameObject.Find("Controller").GetComponent<Controller>();

            Debug.Log("KinectBodyManager::Started");

        }
    }

    void Update()
    {
        if (_Reader != null)
        {

            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }

                frame.GetAndRefreshBodyData(_Data);
                _Closest = findClosestBody(_Data);

                _FloorClipPlane = new UnityEngine.Vector4(frame.FloorClipPlane.X, frame.FloorClipPlane.Y, frame.FloorClipPlane.Z, frame.FloorClipPlane.W);
                calculateKinectProperties();

                frame.Dispose();
                frame = null;
            }


            if (_Closest != null && enableFacetracking)
            {
                if (faceFrameSource.TrackingId != _ClosestBodyId)
                {
                    faceFrameSource.TrackingId = _ClosestBodyId;
                }

                var faceFrame = faceframeReader.AcquireLatestFrame();
                if (faceFrame != null)
                {

                    if (faceFrame.FaceFrameResult != null)
                    {

                        UnityEngine.Quaternion faceRotation = new UnityEngine.Quaternion(faceFrame.FaceFrameResult.FaceRotationQuaternion.X,
                                                                                       faceFrame.FaceFrameResult.FaceRotationQuaternion.Y,
                                                                                       faceFrame.FaceFrameResult.FaceRotationQuaternion.Z,
                                                                                       faceFrame.FaceFrameResult.FaceRotationQuaternion.W);

                        //correct for kinect rotation
                        currentFaceRotation = faceRotation * _kinectRotation;
                    }
                    faceFrame.Dispose();
                    faceFrame = null;
                }
            }

            if (_Closest != null && enableHDFace)
            {
                if (highDefinitionFaceFrameSource.TrackingId != _ClosestBodyId)
                {
                    highDefinitionFaceFrameSource.TrackingId = _ClosestBodyId;
                }

                //get face data
                var HDfaceFrame = highDefinitionFaceFrameReader.AcquireLatestFrame();
                if (HDfaceFrame != null)
                {
                    HDfaceFrame.GetAndRefreshFaceAlignmentResult(currentFaceAlignment);
                    if (currentFaceAlignment != null)
                    {
                        //var vertexCount = FaceModel.VertexCount;

                        if (normals == null)
                        {
                            normals = new Vector3[FaceModel.VertexCount];
                            uvs = new Vector2[FaceModel.VertexCount];
                        }

                        var verticesCameraSpacePoint = currentFaceModel.CalculateVerticesForAlignment(currentFaceAlignment);
                        var vertices = new Vector3[FaceModel.VertexCount];
                        var correctedVertices = new Vector3[FaceModel.VertexCount];
                        _CurrentFacePivot = new Vector3(currentFaceAlignment.HeadPivotPoint.X, currentFaceAlignment.HeadPivotPoint.Y, currentFaceAlignment.HeadPivotPoint.Z);
                        for (int i = 0; i < FaceModel.VertexCount; i++)
                        {
                            //vertices[i] = new Vector3(verticesCameraSpacePoint[i].X - _CurrentFacePivot.x, verticesCameraSpacePoint[i].Y - _CurrentFacePivot.y, verticesCameraSpacePoint[i].Z - _CurrentFacePivot.z);
                            vertices[i] = new Vector3(verticesCameraSpacePoint[i].X, verticesCameraSpacePoint[i].Y, verticesCameraSpacePoint[i].Z);
                            correctedVertices[i] = kinectRotationTransform(vertices[i], _CurrentFacePivot);
                            normals[i] = -Vector3.forward;
                        }
                        _CurrentFaceMeshVertices = vertices;
                        _CurrentFaceMeshCorrectedVertices = correctedVertices;
                    }
                    HDfaceFrame.Dispose();
                    HDfaceFrame = null;
                }
            }
        }
    }

    private void calculateKinectProperties()
    {
        UnityEngine.Vector3 up = _FloorClipPlane;
        UnityEngine.Vector3 forward = UnityEngine.Vector3.forward;
        UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(up, forward);

        // correct forward direction
        forward = UnityEngine.Vector3.Cross(right, up);

        _kinectRotation = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(forward.x, -forward.y, forward.z), new UnityEngine.Vector3(up.x, up.y, -up.z));

        _kinectPosition = Vector3.up * _FloorClipPlane.w;
        _kinectPosition.x = kinectXoffset;

        //move the mirror in the scene to the correct height
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, _kinectPosition.y + kinectYoffset, gameObject.transform.position.z);

        //Debug.Log("_FloorClipPlane:: " + _FloorClipPlane.ToString());
        //Debug.Log("_kinectPosition:: " + _kinectPosition.ToString());
        //Debug.Log("_kinectRotation:: " + _kinectRotation.ToString());
    }


    public Vector3 kinectPossitionTransforms(Vector3 point)
    {
        Vector3 pos = new Vector3(point.x, point.y, point.z);
        pos = _kinectRotation * pos;
        pos = pos + _kinectPosition;
        return pos;
    }
    public Vector3 kinectPossitionTransforms(Body body, Windows.Kinect.JointType joint)
    {
        //Correct for kinect offset
        Vector3 pos = new Vector3(body.Joints[joint].Position.X, body.Joints[joint].Position.Y, body.Joints[joint].Position.Z);
        pos = _kinectRotation * pos;
        pos = pos + _kinectPosition;
        return pos;
    }

    public Vector3 kinectRotationTransform(Vector3 point, Vector3 center)
    {
        //Correct for kinect rotation
        return _kinectRotation * (point - center) + point;
    }

    public Quaternion kinectRotationTransform(Body body, Windows.Kinect.JointType joint)
    {
        //Correct for kinect rotation
        Quaternion rot = new Quaternion(body.JointOrientations[joint].Orientation.X, body.JointOrientations[joint].Orientation.Y, body.JointOrientations[joint].Orientation.Z, body.JointOrientations[joint].Orientation.W);
        return _kinectRotation * rot;
    }


    public Vector3 reflectionTransform(Body body, Windows.Kinect.JointType joint)
    {
        //Caculate Vertual reflected points
        Vector3 pos = kinectPossitionTransforms(body, joint);
        return new Vector3(pos.x, pos.y, -1.0f * pos.z);
    }

    public Vector3 orthogonalSpaceTransform(Body body, Windows.Kinect.JointType joint)
    {

        //Caculate MirrorSpace points
        Vector3 realjointPos = kinectPossitionTransforms(body, joint);
        Vector3 reflectionJointPos = reflectionTransform(body, joint);

        RaycastHit hit;

        Vector3 dir = (reflectionJointPos - realjointPos).normalized;

        //Debug.Log("HeadRayDIR:: " + dir.ToString());

        Ray r = new Ray(realjointPos, dir);

        if (Physics.Raycast(r, out hit, 4.0f, layerMask))
        {
            if (hit.collider.tag == "Mirror")
            {
                return (Vector3)hit.point;
            }
        }

        return Vector3.zero;
    }

    public Vector3 mirrorSpaceTransform(Body body, Windows.Kinect.JointType joint)
    {

        //Caculate MirrorSpace points
        Vector3 realHeadPos = kinectPossitionTransforms(body, JointType.Head);
        Vector3 reflectionJointPos = reflectionTransform(body, joint);
        RaycastHit hit;

        Vector3 dir = (reflectionJointPos - realHeadPos).normalized;

        //Debug.Log("HeadRayDIR:: " + dir.ToString());

        Ray r = new Ray(realHeadPos, dir);

        //TODO should take into acount the rotation of the head - the direction should be betwwen the head and where the look ray intersects with the reflection plane
        //TODO should do this with line intersects plane not raycasting
        if (Physics.Raycast(r, out hit, 4.0f, layerMask))
        {
            if (hit.collider.tag == "Mirror")
            {
                //Debug.Log("HitPoint1::" + hit.point.ToString());
                return (Vector3)hit.point;
            }
        }

        return Vector3.zero;
    }

    Body findClosestBody(Body[] bodies)
    {
        Body closest = null;
        float closestZ = 999;
        if (bodies != null)
        {
            foreach (Body body in bodies)
            {
                if (body.IsTracked)
                {
                    if (enforceInteractionCorridor == true)
                    {
                        if (body.Joints[JointType.SpineBase].Position.Z < closestZ &&
                                (closestZ - body.Joints[JointType.SpineBase].Position.Z) > interactionCorridorZthreshold &&
                                body.Joints[JointType.SpineBase].Position.X > interactionCorridorLeftLimit &&
                                body.Joints[JointType.SpineBase].Position.X < interactionCorridorRightLimit &&
                                body.Joints[JointType.SpineBase].Position.Z < interactionCorridorFarLimit
                            )
                        {
                            closestZ = body.Joints[JointType.SpineBase].Position.Z;
                            closest = body;
                            _ClosestBodyId = closest.TrackingId;
                        }
                    }
                    else
                    {
                        if (body.Joints[JointType.SpineBase].Position.Z + interactionCorridorZthreshold < closestZ)
                        {
                            closestZ = body.Joints[JointType.SpineBase].Position.Z;
                            closest = body;
                            _ClosestBodyId = closest.TrackingId;
                        }
                    }
                }
            }
        }

        if (_Closest == null && closest != null)
        {
            //we have a new user!!
            controller.newUser();
        }

        if (_Closest != null && closest == null)
        {
            controller.userLeft();
        }

        return closest;
    }

    void OnApplicationQuit()
    {

        if (CurrentFaceModel != null)
        {
            CurrentFaceModel.Dispose();
            CurrentFaceModel = null;
        }

        highDefinitionFaceFrameSource = null;

        if (highDefinitionFaceFrameReader != null)
        {
            highDefinitionFaceFrameReader.Dispose();
            highDefinitionFaceFrameReader = null;
        }

        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}