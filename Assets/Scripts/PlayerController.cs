using UnityEngine;

public class PlayerController : PlayerPhysics
{
    // Adjustable parameters 
    [Header("Sling values")]
    [SerializeField] float slingForce;
    [SerializeField] float slingRadius;
    [SerializeField] float slingTimer;
    [SerializeField] GameObject ringTimerObject;   
    [SerializeField] [Tooltip("0 = Time stop, 1 = Normal time")] [Range(0, 1)] float timeSlow = 0.5f;
    [SerializeField] bool directionRelativeToMouse;
    [SerializeField] float groundCheckOffset;
    public bool enableSlinging = true;
    public AudioSource audio;
    public AudioClip slingingSound;
    public AudioClip slingSound;

    Vector2 mouseClickPos;
    Vector2 slingPos;
    public Vector2 slingDir;
    [SerializeField] GameObject dirArrow;
    [SerializeField] GameObject joystick;

    LayerMask groundMask;
    LayerMask wallMask;

    // Debugging 
    [Header("Debug")] 
    [SerializeField] bool sling = true;
    [SerializeField] bool testPCG;
    
    private void Start()
    {
        GetComponent<CircleCollider2D>().radius = slingRadius; // Radius of the circle that manages sling hitbox
        groundMask = LayerMask.GetMask("Ground");
        wallMask = LayerMask.GetMask("Wall");
    }

    private void FixedUpdate()
    {
        HandlePhysics(IsGrounded());
    }

    private void Update()
    {
        Dash(30);

        if (IsGrounded())
        {
            sling = true;
            slingPos = transform.position;
        }
            
        Sling();   
    }

    // Enables slinging when entering sling radius
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Slinger"))
        {
            sling = true;
            slingPos = collision.gameObject.transform.position;

            if (collision.gameObject.GetComponent<BlackHole>().scoreable)
            {
                ScoreManager.Instance.Score++;
                collision.gameObject.GetComponent<BlackHole>().scoreable = false;
            }
                
        }    
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Slinger"))
        {          
            sling = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            InvertForce(new Vector2(Velocity.x * wallBounceForce, -Velocity.y));
        }
    }
    public bool IsGrounded()
    {
        // Throws a raycast downwards, left and right from player and checks for the ground or wall layermask
        if (Velocity.y <= 0 && Physics2D.Raycast(transform.position, Vector2.down, transform.localScale.y / 2 + groundCheckOffset, groundMask))
        {
            return true;
        }
        else
            return false;
    }
    public bool WallCheck()
    {
        return Physics2D.Raycast(transform.position, Vector2.right, transform.localScale.x / 2 + .1f, wallMask) ||
                Physics2D.Raycast(transform.position, Vector2.left, transform.localScale.x / 2 + .1f, wallMask);
    }

    void Dash(float dashForce)
    {
        Vector2 dir = Vector3.Normalize(Velocity);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Velocity += new Vector2(dir.x * 20, 20);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Velocity += new Vector2(dashForce, 0);
        }
    }

    bool CanSling() => sling ? true : false;

    // ---- REFACTOR --------
    void Sling()
    {
        // Executes if player is able to sling
        if (CanSling())
        {
            if(!IsGrounded())
            {
                mouseClickPos = MouseToWorldPos();
                transform.position = Vector3.Lerp(transform.position, slingPos, 0.01f);
                SlingTimer(true);
            }       
            else
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    mouseClickPos = MouseToWorldPos();                    
                }
                // Code block for sling mechanic
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    transform.position = Vector3.Lerp(transform.position, slingPos, 0.01f);

                    // -- Change to better sling animation -- 
                    GetComponent<PlayerBehaviour>().animator.Play("Base Layer.PlayerGroundCollision", 0, 0);
                    //----------------------

                    SlingTimer(true);
                }
            }
            
            // Code block for sling release
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                SlingTimer(false);
                transform.position = slingPos;
            }
        }
        // Executes if player is not able to sling
        else
        {
            //Defaults if player manages to enter and exit sling state without releasing 
            SetSlingValues(Velocity, gravity, 1f);
        }
    }

    void SlingGFX(bool isSlinging)
    {
        if (isSlinging)
        {
            ringTimerObject.transform.localScale -= Vector3.one * 2 * Time.deltaTime / slingTimer;
            ringTimerObject.GetComponent<SpriteRenderer>().color += new Color(1, -1, 0, 0) * Time.deltaTime;
        }
        else
        {
            ringTimerObject.transform.localScale = Vector3.one * 0.5f;
            ringTimerObject.GetComponent<SpriteRenderer>().color = new Color(0, 1, 0, .9f);
        }
    }

    void SlingTimer(bool isSlinging)
    {       
        if (isSlinging && ringTimerObject.transform.localScale.x > 0)
        {
            SetSlingValues(Vector2.zero, 0, timeSlow);
            ActivateDirectionGFX(isSlinging);
            SlingGFX(isSlinging);
        }
        else
        {
            sling = false;
            isSlinging = false;
            SetSlingValues(slingForce * Direction(), gravity, 1f);
            SlingGFX(isSlinging);
            audio.Stop();
            audio.PlayOneShot(slingSound);
        }

        ActivateDirectionGFX(isSlinging);
        ringTimerObject.SetActive(isSlinging);
    }

    Vector2 MouseToWorldPos()
    {
        Vector2 mousePos = Input.mousePosition;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    void SetSlingValues(Vector2 velocity, float gravity, float timeScale)
    {      
        Velocity = velocity;
        Gravity = gravity;
        Time.timeScale = timeScale;
    }

    // Line renderer temporarily shows the direction before adding sweet ass gfx
    void ActivateDirectionGFX(bool active)
    {
        if (active)
        {
            dirArrow.transform.position = slingPos;

            Quaternion target = Quaternion.Euler(0, 0, Mathf.Atan2(-Direction().x, Direction().y) * Mathf.Rad2Deg);

            dirArrow.transform.rotation = target;
            dirArrow.SetActive(true);
        }
        else
            dirArrow.SetActive(false);
    }

    // Return the direction of the sling by normalizing the inverse of the direction from the player position to the mouse position
    public Vector2 Direction()
    {
        /*Vector3 direction;
        Vector3 mouseWorldPos = MouseToWorldPos();

        // Get direction relative to initial mouse pos
        if (directionRelativeToMouse)
            direction = Vector3.Normalize(new Vector3(mouseClickPos.x, mouseClickPos.y, 0) - new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0));
        // Get direction relative to player posititon
        else
            direction = Vector3.Normalize(new Vector3(transform.position.x, transform.position.y, 0) - new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0));*/

        Vector3 pointA = joystick.transform.position;
        Vector3 pointB = MouseToWorldPos();
 
        if (Vector3.Distance(pointA, pointB) < 5)
        {
            slingDir = Vector3.Normalize(new Vector3(pointA.x, pointA.y, 0) - new Vector3(pointB.x, pointB.y, 0));
        }
           
        return new Vector2(slingDir.x, Mathf.Clamp(slingDir.y, 0, 1));
    }
}






