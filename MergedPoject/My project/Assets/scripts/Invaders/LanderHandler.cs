using UnityEngine;

public class LanderHandler : MonoBehaviour {
    [SerializeField] private int handlerID = 0;

    [Header("Grid Settings")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 11;
    [SerializeField] private float degreesBetweenInvaders = 5f;
    [SerializeField] private float heightPadding = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float startingDegree = 0f;
    [SerializeField] private float startingDirection = 1f;
    [SerializeField] private float dropAmount = 1f;
    [SerializeField] private float bounceAmount = 1f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float incrementSpeedBy = 0.25f;
    [SerializeField] private AnimationCurve moveSpeed;
    [SerializeField] private AnimationCurve bounceSpeed;

    //[Header("Projectile Settings")]

    [Header("Spawn Movement")]
    [SerializeField] private float moveIntoFrameSpeed = 1f;
    [SerializeField] private float moveIntoFrameAmount = 10f;

    [Header("Prefab")]
    [SerializeField] private GameObject BasicLanderInvader;
    [SerializeField] private GameObject SpecialLanderInvader;
    [SerializeField] private Projectile missile;
    [SerializeField] private Transform projectileHolder;

    private float currentMoveIntoFrameAmount = 0f;
    private float currentDropAmount = 0f;
    private float currentBounceAmount = 0f;
    private bool isMovingDown = false;
    private bool isMovingUp = false;
    private float increaseBaseSpeedPercent = 1;
    // counting 
    public int amountKilled { get; private set; }
    private int totalAmountInvaders => rows * columns;
    private int amoutAlive => totalAmountInvaders - amountKilled;

    private float percentKilled => (float)amountKilled / (float)totalAmountInvaders;

    // firing projectiles
    //private 
    private enum Pattern {
        ZigZag,
        Rain,
        Wild
    }
    private Pattern pattern;
    private int yRow = 0;
    private int xColumn = 0;
    private int fireDirection = 0;
    private int stayDuration = 0;

    private GameObject[,] InvaderGrid;
    private GameObject[] RadiusParents;


    private void Awake() {
        InvaderGrid = new GameObject[columns, rows];
        RadiusParents = new GameObject[columns];
    }

    private void Start() {
        GameStateManager.Instance.OnStateChanged += GameStateManager_OnStateChanged;
        ProjectileManager.Instance.OnProjectileTick += ProjectileManager_OnProjectileTick;

    }

    private void GameStateManager_OnStateChanged(object sender, System.EventArgs e) {
        if (!GameStateManager.Instance.IsGameCountdownToStart()) { return; }
        increaseBaseSpeedPercent -= incrementSpeedBy;
        StartLanders();
        RandomizeFire();
    }

    private void StartLanders() {
        currentMoveIntoFrameAmount = moveIntoFrameAmount;
        amountKilled = 0;
        increaseBaseSpeedPercent += incrementSpeedBy;
        this.transform.eulerAngles = Vector3.zero;


        // populate grid with invaders

        for (int col = 0; col < this.columns; col++) {

            RadiusParents[col] = Instantiate(new GameObject("RadiusParentLander"), Vector3.zero, Quaternion.identity, this.transform);

            for (int row = 0; row < rows; row++) {
                if (row == rows / 2) {
                    InvaderGrid[col, row] = Instantiate(SpecialLanderInvader, Vector3.zero, Quaternion.identity, RadiusParents[col].transform);
                    // action
                    SpecialLanderInvader currentInvader = InvaderGrid[col, row].GetComponent<SpecialLanderInvader>();
                    if (!currentInvader) { Debug.LogError("missing SpecialLanderInvader script"); }
                    currentInvader.killed += InvaderKilled;
                    currentInvader.bounce += BounceGrid;
                } else {
                    InvaderGrid[col, row] = Instantiate(BasicLanderInvader, Vector3.zero, Quaternion.identity, RadiusParents[col].transform);
                    // action
                    LanderInvader currentInvader = InvaderGrid[col, row].GetComponent<LanderInvader>();
                    if (!currentInvader) { Debug.LogError("missing LanderInvader script"); }
                    currentInvader.killed += InvaderKilled;
                    currentInvader.bounce += BounceGrid;
                }


                InvaderGrid[col, row].transform.localPosition += new Vector3(0, CameraManager.Instance.viewportRadius + row * heightPadding, 0);

            }

            RadiusParents[col].transform.Rotate(0, 0, degreesBetweenInvaders * col + 45 + 45 - (columns * degreesBetweenInvaders) / 2 );
        }

        this.transform.eulerAngles = new Vector3(0, 0, startingDegree - 90);
    }

    private void DestroyLanders() {

        for (int col = 0; col < this.columns; col++) {
            Destroy(RadiusParents[col]);
            for (int row = 0; row < rows; row++) {
                Destroy(InvaderGrid[col, row]);
            }
        }

    }

    private void Update() {

        if (!GameStateManager.Instance.IsGamePlaying() && !GameStateManager.Instance.IsGameCountdownToStart()) { return; }

        if (currentMoveIntoFrameAmount > 0) {
            float moveAmountThisFrame = moveIntoFrameSpeed * Time.deltaTime;
            currentMoveIntoFrameAmount -= moveAmountThisFrame;
            foreach (GameObject invader in InvaderGrid) {
                if (!invader) { continue; }
                invader.transform.localPosition -= new Vector3(0, moveAmountThisFrame, 0);
            }

            if (currentMoveIntoFrameAmount < 0) {
                foreach (GameObject invader in InvaderGrid) {
                    if (!invader) { continue; }
                    invader.transform.localPosition -= new Vector3(0, currentMoveIntoFrameAmount, 0);
                }
                if (GameStateManager.Instance.IsGameCountdownToStart())
                    GameStateManager.Instance.EndCountdownToStart();
            }
        }

        if (GameStateManager.Instance.IsGameCountdownToStart()) { return; }

        //move up
        if (isMovingUp) {


            if (currentBounceAmount < bounceAmount) {
                float moveAmountThisFrame = bounceSpeed.Evaluate(currentBounceAmount / bounceAmount) * Time.deltaTime;
                currentBounceAmount += moveAmountThisFrame;
                foreach (GameObject invader in InvaderGrid) {
                    invader.transform.localPosition += new Vector3(0, moveAmountThisFrame, 0);
                }
                return;
            }

            if (currentBounceAmount > bounceAmount) {
                foreach (GameObject invader in InvaderGrid) {
                    invader.transform.localPosition -= new Vector3(0, currentBounceAmount - bounceAmount, 0);
                }
            }

            isMovingUp = false;
            currentBounceAmount = 0;
        }

        //move down
        if (isMovingDown) {


            if (currentDropAmount > 0) {
                float moveAmountThisFrame = moveSpeed.Evaluate(percentKilled) * increaseBaseSpeedPercent * Time.deltaTime;
                currentDropAmount -= moveAmountThisFrame;
                foreach (GameObject invader in InvaderGrid) {
                    invader.transform.localPosition -= new Vector3(0, moveAmountThisFrame, 0);
                }
                return;
            }

            if (currentDropAmount < 0) {
                foreach (GameObject invader in InvaderGrid) {
                    invader.transform.localPosition -= new Vector3(0, currentDropAmount, 0);
                }
            }

            isMovingDown = false;
            currentDropAmount = dropAmount;
        }




        //this.transform.position += direction * moveSpeed.Evaluate(percentKilled) * IncreaseBaseSpeedPercent * Time.deltaTime;

        // move left or right
        foreach (GameObject invader in InvaderGrid) {
            // if invader is dead, skip and continue to next invader
            if (!invader.gameObject.activeInHierarchy) {
                continue;
            }

            if (startingDirection < 0 && invader.transform.parent.transform.localEulerAngles.z < 50) {
                // hit right most limits
                // flip direction and move down
                startingDirection *= -1f;
                isMovingDown = true;
                //Debug.Log("RIGHT edge hit");
                return;
            } else if (startingDirection > 0 && invader.transform.parent.transform.localEulerAngles.z > 130) {
                // hit left most limits
                // flip direction and move down
                startingDirection *= -1f;
                isMovingDown = true;
                //Debug.Log("LEFT edge hit");
                return;
            }
        }
        //Debug.Log(invader.transform.parent.transform.localEulerAngles.z);
        //Debug.Log(startingDirection * moveSpeed.Evaluate(percentKilled) * increaseBaseSpeedPercent * rotationSpeed * Time.deltaTime);
        float rotationAmountThisFrame = moveSpeed.Evaluate(percentKilled) * increaseBaseSpeedPercent * rotationSpeed * Time.deltaTime;
        foreach (GameObject parent in RadiusParents) {
            parent.transform.Rotate(0, 0, startingDirection * rotationAmountThisFrame);
        }

    }



    private void ProjectileManager_OnProjectileTick(object sender, System.EventArgs e) {
        if (ProjectileManager.Instance.selectedHandler != handlerID) { return; }

        switch (pattern) {
            case Pattern.ZigZag:
                ZigZag();

                break;
            case Pattern.Rain:
                Rain();

                break;
            case Pattern.Wild:
                Wild();

                break;
        }
    }
    private bool FireMissle() {
        if (!InvaderGrid[xColumn, yRow].gameObject.activeInHierarchy) { return false; }
        Instantiate(missile, InvaderGrid[xColumn, yRow].transform.position, Quaternion.identity, projectileHolder);
        return true;
    }

    private void RandomizeFire() {
        int randomPattern = Random.Range(0, 3);
        switch (randomPattern) {
            case 0: pattern = Pattern.ZigZag; break;
            case 1: pattern = Pattern.Rain; break;
            case 2: pattern = Pattern.Wild; break;
        }
        xColumn = Random.Range(0, columns);
        yRow = rows - 1;
        fireDirection = Random.Range(0, 2);
        stayDuration = Random.Range(4, 12);
    }

    private void ZigZag() {
        bool isfired = false;
        do {
            isfired = FireMissle();
            if (fireDirection == 0) {
                if (xColumn <= 0) {
                    fireDirection = 1;
                    xColumn++;
                    yRow--;
                } else {
                    xColumn--;
                }
            } else {
                if (xColumn >= columns - 1) {
                    fireDirection = 0;
                    xColumn--;
                    yRow--;
                } else {
                    xColumn++;
                }
            }

            if (yRow < 0) {
                RandomizeFire();
                ProjectileManager.Instance.SelectNewHandler();
                break;
            }
        } while (!isfired);
    }

    private void Rain() {;
        bool isfired = false;
        do {
            isfired = FireMissle();

            if (stayDuration > 0) {
                if (yRow <= 0) {
                    stayDuration--;
                    yRow = rows - 1;
                    xColumn = Random.Range(0, columns);
                } else {
                    yRow--;
                }
            } else {
                RandomizeFire();
                ProjectileManager.Instance.SelectNewHandler();
                break;
            }
        } while (!isfired);

    }

    private void Wild() {
        bool isfired = false;
        do {
            isfired = FireMissle();
            fireDirection = Random.Range(0, 2);
            if (fireDirection == 0) {
                if (xColumn <= 0) {
                    xColumn++;
                    yRow--;
                } else {
                    xColumn--;
                }
            } else {
                if (xColumn >= columns - 1) {
                    fireDirection = 0;
                    xColumn--;
                    yRow--;
                } else {
                    xColumn++;
                }
            }

            if (yRow < 0) {
                RandomizeFire();
                ProjectileManager.Instance.SelectNewHandler();
                break;
            }
        } while (!isfired);
    }

    private void InvaderKilled() {
        amountKilled++;
        if (amountKilled >= totalAmountInvaders) {
            ScoreManager.Instance.AddToScore(1000);

            DestroyLanders();
            StartLanders();
        }
    }

    private void BounceGrid() {

        isMovingUp = true;

    }

}
