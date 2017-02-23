using UnityEngine;
using System.Collections;
using Exploder;
using ProgressBar;

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
        public int score = 0;
        private float _delay = 2.5f;
        private float game_start_time = 0;
        
        private GameObject _currentShip;
        public ProgressBarBehaviour BarBehaviour;

        // Use this for initialization
        public override void Start()
        {

            lastTime = 0;
            lives = 0;
            game_start_time = Time.time;
            newUser();
            Physics.gravity = new Vector3(Physics.gravity.x, starting_gravity, Physics.gravity.z);

            interactionGameObjectScript = interactionGameObject.GetComponent<trackJoint>();
            interactionGameObjectScript.interactionStyle = "mirrorSpacePosition";
 
        }

        public override void newUser()
        {
            if (lives <= 0)
            {
                lastTime = 0;
                score = 0;
                lives = numberOfLives;
                game_start_time = Time.time;
                Physics.gravity = new Vector3(Physics.gravity.x, -1.0f, Physics.gravity.z);
                _delay = inital_fall_delay;
                if(_currentShip == null) {
                    _currentShip = Instantiate(shipPrototype);
                    _currentShip.transform.position = SHIP_START_POS;
                }
            }
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
            /*lives -= 1;
            if (lives < 1) 
            {
                print("Game Over");
                gameOver();
            }*/
        }
        
        public void rockDestroyed() 
        {
            score = score + rock_destroyed_score;
        }

        public override void gameOver()
        {

            if (_currentShip != null) 
            {
                exploder.transform.position = _currentShip.transform.position;
                exploder.ExplodeObject(_currentShip,null);
                Destroy(_currentShip);
                _currentShip = null;
            }
        }

        // Update is called once per frame
        private float timeRemaining;
        public override void Update()
        {

            timeRemaining = (game_start_time + WAVE_LENGTH_SECONDS) - Time.time;
            if (Time.time > lastTime + _delay)
            {
                genCube();
                lastTime = Time.time;
            }

            if(timeRemaining < 50.0)
            {
                interactionGameObjectScript.interactionStyle = "orthogonalMirrorSpace";
            }

            if (timeRemaining <= 0 )
            {
                lives = 0;
                gameOver();
            }

        }

        
        public override void OnGUI()
        {
            GUIStyle LabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
            LabelStyle.fontSize = 32;
            LabelStyle.normal.textColor = Color.yellow;

            if (lives <= 0)
            {
                GUIStyle GOLabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
                GOLabelStyle.fontSize = 52;
                GOLabelStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(400, 650, 300, 80), "Game Over", GOLabelStyle);
                GUI.Label(new Rect(350, 750, 500, 80), "Step up to start a new game", LabelStyle);
            }
            else
            {
                
                GUI.Label(new Rect(15, 10, 200, 60), "Score: " + score, LabelStyle);
                GUI.Label(new Rect(15, 40, 400, 60), "Time remaining: " + System.Math.Round(timeRemaining, 1), LabelStyle);

                if (score < BAD_SCORE)
                {
                    BarBehaviour.Text = "Try Harder :-(";
                    BarBehaviour.Value = 66;
                }
                else if (score > GOOD_SCORE)
                {
                    BarBehaviour.Text = "Doing well :-)";
                    BarBehaviour.Value = 0;
                }
                else
                {
                    BarBehaviour.Text = "Could be better :-|";
                    BarBehaviour.Value = 33;
                }
            }

            
        }

        void genCube()
        {
            if (lives > 0)
            {
                score += 1;

                float game_length = (Time.time - game_start_time);

                float i = 0;

                if (stop_increasespeed_dificulty_after_x_seconds < game_length)
                {
                    i = game_length / 20;
                }
                else
                {
                    i = stop_increasespeed_dificulty_after_x_seconds / 20;
                }

                
                for (var j = 0; j <= i; j++)
                {
                    GameObject newCube = Instantiate(fallingThingPrototype);
                    float offset = Random.Range(-0.1f, 0.1f);
                    float offsety = Random.Range(0.0f, fallingThingsGenYOffset);
                    newCube.transform.position = new Vector3(kinnect.transform.position.x + offset, kinnect.transform.position.y + offsety, kinnect.transform.position.z);
                    Destroy(newCube, 10);    
                }

                
                if (game_length % increase_speed_after_x_seconds == 0 && stop_increasespeed_dificulty_after_x_seconds < game_length)
                {
                    _delay -= 0.1f;
                    Physics.gravity = new Vector3(Physics.gravity.x, Physics.gravity.y - increaseGravityBy, Physics.gravity.z);
                }

            }
        }
    }

