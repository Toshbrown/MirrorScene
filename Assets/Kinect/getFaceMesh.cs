using UnityEngine;
using System.Collections;

public class getFaceMesh : MonoBehaviour {

    private Controller controller;
    private KinectBodyManager KBM;
    private GameObject KinectAndScreen;

    public bool useCorrectedVerts;

    private Mesh Face;
    void Start()
    {
        controller = GameObject.Find("Controller").GetComponent<Controller>();
        KinectAndScreen = GameObject.Find("KinectAndScreen");
        KBM = KinectAndScreen.GetComponent<KinectBodyManager>();

        Face = new Mesh();
        Face.MarkDynamic();
        
    }

	
	// Update is called once per frame
	void Update () {

        Vector3[] verts;
        if(useCorrectedVerts) {
            verts = KBM.GetClosestCorrectedFaceVertices();

        } 
        else 
        {
            verts = KBM.GetClosestFaceVertices();
        }
        
        if (verts != null && verts.Length > 0)
        {
            //Face.vertices = KBM.GetClosestFaceVertices();
            Face.vertices = verts;
            Face.normals = KBM.GetClosestFaceNormals();
            Face.uv = KBM.GetClosestFaceUVs();
            Face.triangles = KBM.GetClosestFaceTriangles();
            Face.RecalculateNormals();
            Face.RecalculateBounds();
            gameObject.GetComponent<MeshFilter>().mesh = Face;
        }
        

    }
}
