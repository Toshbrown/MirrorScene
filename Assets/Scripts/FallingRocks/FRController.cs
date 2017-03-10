using UnityEngine;
using System.Collections;
using Exploder;
using System.Collections.Generic;
using System.IO;
using System;
    



public class FRController : Controller
{

    public ExploderObject exploder;
    public GameObject fallingThingPrototype;
    public GameObject kinnect;
    public GameObject shipPrototype;
    public GameObject interactionGameObject;
    private trackJoint interactionGameObjectScript;

    public int WAVE_LENGTH_SECONDS = 60;
    public Vector3 SHIP_START_POS = new Vector3(0.0f, 1.3f, 0.0f);
    public float inital_fall_delay = 2.5f;
    public int increase_speed_after_x_seconds = 5;
    public int stop_increasespeed_dificulty_after_x_seconds = 25;
    public float starting_gravity = 7.0f;
    public float increaseGravityBy = 0.02f;

    public int BAD_SCORE = -100;
    public int GOOD_SCORE = 100;

    public int ship_hit_score = -100;
    public int rock_destroyed_score = 10;

    private float lastTime;

    public int numberOfLives = 3;
    public float fallingThingsGenYOffset = 0.3f;
    private int lives = 0;
    private float _delay = 2.5f;


    public int score = 0;
    public int rocksSporned = 0;
    public int rocksDestroyed = 0;


    private GameObject _currentShip;

    List<string> _currentState;

    List<string> states = new List<string> { "waitingForUser", "wave1", "wave2", "rest" ,"wave3", "wave4", "gameover" };

    string logFileName = @"c:\users\public\falingRocks.log";
    // Use this for initialization
    public override void Start()
    {

        lastTime = 0;
        lives = 0;
        Physics.gravity = new Vector3(Physics.gravity.x, starting_gravity, Physics.gravity.z);

        interactionGameObjectScript = interactionGameObject.GetComponent<trackJoint>();
        interactionGameObjectScript.interactionStyle = "mirrorSpacePosition";

        _currentState = states;

    }

    public override void newUser()
    {
        if (lives <= 0)
        {

            _currentState = states;

            //remove waitingForUser state
            _currentState.RemoveAt(0);

            lastStateChageTime = 0;
            lastTime = 0;
            score = 0;
            rocksSporned = 0;
            rocksDestroyed = 0;
            lives = numberOfLives;
            Physics.gravity = new Vector3(Physics.gravity.x, -1.0f, Physics.gravity.z);
            _delay = inital_fall_delay;
            if (_currentShip == null)
            {
                _currentShip = Instantiate(shipPrototype);
                _currentShip.transform.position = SHIP_START_POS;
            }

            interactionGameObjectScript.interactionStyle = "mirrorSpacePosition";

            File.AppendAllText(logFileName, "######  NEW USER " + DateTime.Now.ToShortTimeString() + " ######\n");
        }
    }

    float lastStateChageTime = 0;
    public void nextState()
    {
        if (_currentState[0] != "waitingForUser" && (Time.time - lastStateChageTime) > WAVE_LENGTH_SECONDS)
        {

            //reset rock count for each state
            rocksSporned = 0;
            rocksDestroyed = 0;

            //swap interaction style after rest
            if (_currentState[0] == "rest")
            {
                interactionGameObjectScript.interactionStyle = "orthogonalMirrorSpace";
                Physics.gravity = new Vector3(Physics.gravity.x, -1.0f, Physics.gravity.z);
                _delay = inital_fall_delay;
            }

            if (_currentState[0] == "gameover")
            {
                _currentState = states;
                lastStateChageTime = Time.time;
            }
            else 
            {
                _currentState.RemoveAt(0);
                lastStateChageTime = Time.time;
            }

            //destroy all the things when entering rest state
            if (_currentState[0] == "rest")
            {
                GameObject[] rocks = GameObject.FindGameObjectsWithTag("FallingThing");
                foreach (GameObject rock in rocks)
                {
                    Destroy(rock,1);
                }
            }

            File.AppendAllText(logFileName, "######  " + _currentState[0] + " ######\n");
            Debug.Log("[CHANGED STATE] " + _currentState[0]);
        }
    }

    public void setState(string state){
    
        _currentState[0] = state;
        lastStateChageTime = Time.time;
        File.AppendAllText(logFileName, "######  " + _currentState[0] + " ######\n");
    }

    public override void userLeft()
    {
        lives = 0;
        gameOver();
    }

    public override void removeLife()
    {
        //SHIP HIT
        score = score + ship_hit_score;
 
    }

    public void rockDestroyed()
    {
        score = score + rock_destroyed_score;
        rocksDestroyed += 1;
    }

    public override void gameOver()
    {

        if (_currentShip != null)
        {
            exploder.ExplodeObject(_currentShip);
            Destroy(_currentShip,5);
            _currentShip = null;

            setState("gameover");
        }
    }

    // Update is called once per frame
    private float lastLogTime = 0;
    public override void Update()
    {

        //change state if needed
        nextState();

        if(_currentState[0] == "gameover") {
            return;
        }

        if (_currentState[0] == "waitingForUser")
        {
            return;
        }

        if (_currentState[0] == "rest")
        {
            return;
        }

        if (Time.time > lastTime + _delay)
        {
            genCube();
            lastTime = Time.time;
        }

        if (Time.time > lastLogTime + 1.0f)
        {
           lastLogTime = Time.time;
           File.AppendAllText(logFileName, Time.time.ToString() + "," + rocksSporned + "," + rocksDestroyed + "\n");
        }
    }


    public override void OnGUI()
    {
        GUIStyle LabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
        LabelStyle.fontSize = 52;
        LabelStyle.normal.textColor = Color.yellow;

        if (_currentState[0] == "gameover")
        {
            GUIStyle GOLabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
            GOLabelStyle.fontSize = 52;
            GOLabelStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(400, 650, 300, 80), "Game Over", GOLabelStyle);

            return;
        }

        if (_currentState[0] == "waitingForUser")
        {
            LabelStyle.fontSize = 32;
            GUI.Label(new Rect(350, 750, 500, 80), "Step up to start a new game", LabelStyle);
            return;
        }

        if (_currentState[0] == "rest")
        {
            LabelStyle.fontSize = 32;
            GUI.Label(new Rect(350, 750, 500, 80), "Have a brake. Relax.", LabelStyle);
            return;
        }

        //the game is afoot

        GUI.Label(new Rect(15, 10, 200, 60), "Score: " + score, LabelStyle);
        if (rocksSporned == 0)
        {
            GUI.Label(new Rect(15, 40, 400, 60), "Perfomance: 0 %", LabelStyle);
        }
        else 
        {
            GUI.Label(new Rect(15, 40, 400, 60), "Perfomance: " + System.Math.Round((rocksDestroyed/rocksSporned)*100.0f, 1) + " %", LabelStyle);
        }
    }

    void genCube()
    {
        

            float i = 0;

            if (_currentState[0] == "wave1" || _currentState[0] == "wave3")
            {
                //make it harder
                i = (Time.time - lastStateChageTime) / 20;
                if (i > 6)
                {
                    i = 6;
                }
                if(_delay > 1.5f) {
                    _delay -= 0.1f;
                }
                if (Physics.gravity.y > -2)
                {
                    Physics.gravity = new Vector3(Physics.gravity.x, Physics.gravity.y - increaseGravityBy, Physics.gravity.z);
                }
            }
            else
            {
                i = WAVE_LENGTH_SECONDS / 20;
            }


            for (var j = 0; j <= i; j++)
            {
                GameObject newCube = Instantiate(fallingThingPrototype);
                fallingThingColoider fallingThingScript = newCube.GetComponent<fallingThingColoider>();

                if(_currentState[0] == "wave2" || _currentState[0] == "wave4") {
                    int number = UnityEngine.Random.Range(1, 101);
                    if (number %2 == 1)
                    {
                        rocksSporned += 1;
                    }
                    fallingThingScript.setNumber(UnityEngine.Random.Range(1, 101));
                }
                else
                {
                    rocksSporned += 1;
                }

                float offset = UnityEngine.Random.Range(-0.12f, 0.1f);
                float offsety = UnityEngine.Random.Range(0.0f, fallingThingsGenYOffset);
                newCube.transform.position = new Vector3(kinnect.transform.position.x + offset, kinnect.transform.position.y + offsety, kinnect.transform.position.z);
                Destroy(newCube, 10);
            }

    }
}

