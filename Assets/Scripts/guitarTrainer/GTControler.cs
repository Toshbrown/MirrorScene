using UnityEngine;
using System.Collections;

public class GTControler : Controller {

    public MovieTexture neckMovie = null;

	// Use this for initialization
	void Start () {
	    
        if(neckMovie != null) 
        {
            neckMovie.Play();
        }

	}
	
	// Update is called once per frame
	void Update () {
	
	}


}
