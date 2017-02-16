using UnityEngine;
using System.Collections;
using Exploder;

public class FRController : Controller
    {

        public ExploderObject exploder;
        public GameObject fallingThingPrototype;
        public GameObject kinnect;
        public GameObject shipPrototype;

        private float lastTime;

        public float delay = 2.5f;
        public int numberOfLives = 3;
        public int increaseSpeedAfterXpoints = 5;
        public float increaseGravityBy = 0.02f;
        public float fallingThingsGenYOffset = 0.3f;
        private int lives = 0;
        public int score = 0;
        private float _delay = 2.5f;

        private GameObject _currentShip;

        // Use this for initialization
        public override void Start()
        {
            lastTime = 0;
            newUser();
        }

        public override void newUser()
        {
            if (lives <= 0)
            {
                lastTime = 0;
                score = 0;
                lives = numberOfLives;
                Physics.gravity = new Vector3(Physics.gravity.x, -1.0f, Physics.gravity.z);
                _delay = delay;
                if(_currentShip == null) {
                    _currentShip = Instantiate(shipPrototype);
                    _currentShip.transform.position = new Vector3(0.0f, 1.3f, 0.0f);
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
            lives -= 1;
            if (lives < 1) 
            {
                print("Game Over");
                gameOver();
            }
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
        public override void Update()
        {
            if (Time.time > lastTime + _delay)
            {
                genCube();
                lastTime = Time.time;
            }
        }


        public override void OnGUI()
        {
            GUIStyle LabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
            LabelStyle.fontSize = 32;
            LabelStyle.normal.textColor = Color.yellow;

            GUI.Label(new Rect(10, 10, 200, 60), "Score: " + score, LabelStyle);
            GUI.Label(new Rect(10, 40, 200, 60), "Lives: " + lives, LabelStyle);

            if (lives <= 0)
            {
                GUIStyle GOLabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
                GOLabelStyle.fontSize = 52;
                GOLabelStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(400, 650, 300, 80), "Game Over", GOLabelStyle);
                GUI.Label(new Rect(350, 750, 500, 80), "Step up to start a new game", LabelStyle);
            }
        }

        void genCube()
        {
            if (lives > 0)
            {
                score += 1;

                var i = score / 10;
                for (var j = 0; j <= i; j++)
                {
                    GameObject newCube = Instantiate(fallingThingPrototype);
                    float offset = Random.Range(-0.1f, 0.1f);
                    float offsety = Random.Range(0.0f, fallingThingsGenYOffset);
                    newCube.transform.position = new Vector3(kinnect.transform.position.x + offset, kinnect.transform.position.y + offsety, kinnect.transform.position.z);
                    Destroy(newCube, 10);    
                }


                if (score % increaseSpeedAfterXpoints == 0 && _delay > 0.1)
                {
                    _delay -= 0.1f;
                    Physics.gravity = new Vector3(Physics.gravity.x, Physics.gravity.y - increaseGravityBy, Physics.gravity.z);
                }

            }
        }
    }

