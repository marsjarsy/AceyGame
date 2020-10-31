using Elendow.SpritedowAnimator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //note to self, add the ears to this script to be able to move them if acey ever needs to teleport



    [Header("Player Attributes")]
    public float moveSpeed = 1f;
    public float jumpForce = 12f;
    public float wallJumpForce = 5f;
    public int wallJumpTimer = 3;
    public float wallJumpCounter = 0;
    public float gravity = 5;
    public float jumpPeakGravityScale = 2;

    public float maxSlopeAngle = 60f;
    public float slopeSideAngle = 0f;
    public float slopeRayLength = 1;
    public float coyoteBufferLength = 5;
    public float jumpBufferLength = 5;

    [Header("Player Variables")]
    private float jump = 0;
    private bool isDashing = false;
    private bool isDashJumping = false;
    private bool hasDashed = false;
    private bool isSliding = false;
    public bool isGrounded = false;
    public bool hasJumped = false;
    public bool onSlope = false;
    public bool canWalkOnSlope = true;
    public bool isJumping = false;
    public bool isWallJumping = false;
    public float jumpBuffer = 0;
    public float coyoteBuffer = 0;
    private float coyoteY = 0;


    public bool startJump = false;

    [Header("Player Components")]

    public SpriteAnimator animator;


    [SerializeField]
    private PhysicsMaterial2D noFriction;
    [SerializeField]
    private PhysicsMaterial2D fullFriction;

    public GameObject leftEar;
    public GameObject rightEar;
    public GameObject leftEarTarget;
    public GameObject rightEarTarget;

    public ParticleSystem slideParticles;
    public ParticleSystem jumpBurstParticle;
    public ParticleSystem jumpParticles;
    public ParticleSystem dashBurstParticle;


    private Vector2 inputs = Vector2.zero;
    private bool jumpKey = false;
    private bool dashKey = false;
    public Rigidbody2D rb;

    public Transform leftTransform;
    public Transform rightTransform;
    public Transform groundTransform;



    public float groundCheckSize = .25f;

    public float boxSizeX = .95f;

    private bool lastOnGround = false;

    //https://youtu.be/QPiZSTEuZnw
    public float slopeDownAngle;
    private Vector2 slopeNormalPerp;
    private float slopeDownAngleOld;

    public LayerMask ground;

    public AfterImageController testController;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<SpriteAnimator>();

    }
    private void Update()
    {
        //debug thing,remove later please
        if (Input.GetKeyDown(KeyCode.C))
        {

            if (Time.timeScale == .1f)
            {
                Time.timeScale = 1;
            }
            else
            {
                Time.timeScale = .1f;
            }
        }
        jumpKey = Input.GetKey(KeyCode.Z);
        dashKey = Input.GetKey(KeyCode.X);


        //allows you to cancel the wall jump cooldown thing
        if (!jumpKey)
            wallJumpCounter = 0;


        inputs = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        //add a seperate thing later on to support custom controls
        if (Input.GetKeyDown(KeyCode.Z) && hasJumped)
        {
            jumpBuffer = jumpBufferLength;
            Debug.Log("button pushed");
        }
    }

    private float wallJumpCount = 0;
    private int frames = 0;
    private void FixedUpdate()
    {
        //the player will reset to this position upon attempting a coyote jump
        if (isGrounded)
        {
            coyoteY = transform.position.y;
        }

        frames++;
        //isGrounded = Physics2D.OverlapCircle(groundTransform.position, 0, ground);
        if (startJump)
        {
            //start jump basically just checks if you started jumping this frame
            //if yes, it ignores the ground check just to ensure you can get off the ground
            //the entire ground check stuff needs to be cleaned up, but this should be ok
            isGrounded = false;
            startJump = false;
        }
        else
        {
            isGrounded = Physics2D.OverlapCircle(groundTransform.position, groundCheckSize, ground);
        }

        SlopeCheck();
        MovePlayer();
        if (wallJumpCount > 0)
        {
            wallJumpCount -= .1f;
        }
        else
        {
            isWallJumping = false;
        }
        ControlAnimations();

        if (isDashing && frames % 2 == 0)
        {
            Color dashColor = new Color(.25f, 1, 1, 1);
            //creates after images
            Instantiate(testController).CreateAfterImage(dashColor, 10f, new Vector3(transform.position.x, transform.position.y, transform.position.z + 1),
            animator.SpriteRenderer.sprite, GetComponent<SpriteRenderer>().flipX);
            Instantiate(testController).CreateAfterImage(dashColor, 10f, new Vector3(rightEar.transform.position.x, rightEar.transform.position.y, transform.position.z + 1),
            rightEar.GetComponent<SpriteRenderer>().sprite, GetComponent<SpriteRenderer>().flipX);
            Instantiate(testController).CreateAfterImage(dashColor, 10f, new Vector3(leftEar.transform.position.x, leftEar.transform.position.y, transform.position.z + 1),
            leftEar.GetComponent<SpriteRenderer>().sprite, GetComponent<SpriteRenderer>().flipX);
        }

    }

    private void SlopeCheck()
    {
        Vector2 rayPosition = transform.position - new Vector3(0, .49f);

        RaycastHit2D hit = Physics2D.Raycast(rayPosition, Vector2.down, 1, ground);

        RaycastHit2D hitLeft = Physics2D.Raycast(rayPosition, -transform.right, slopeRayLength, ground);
        RaycastHit2D hitRight = Physics2D.Raycast(rayPosition, transform.right, slopeRayLength, ground);

        //horizontal
        if (hitRight)
        {
            onSlope = true;
            slopeSideAngle = Vector2.Angle(hitRight.normal, Vector2.up);
        }
        else if (hitLeft)
        {
            onSlope = true;
            slopeSideAngle = Vector2.Angle(hitLeft.normal, Vector2.up);
        }
        else
        {
            onSlope = false;
            slopeSideAngle = 0;
        }

        //vertical
        if (hit)
        {

            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != slopeDownAngleOld)
            {
                onSlope = true;
            }

            slopeDownAngleOld = slopeDownAngle;
            Debug.DrawRay(hit.point, hit.normal, Color.green);
            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
        }
        if ((slopeSideAngle < maxSlopeAngle || slopeDownAngle < 10) && isGrounded)
        {
            canWalkOnSlope = true;
        }
        else
        {
            canWalkOnSlope = false;
        }

        if (onSlope && inputs.x == 0 && canWalkOnSlope && !isDashing)
        {
            rb.sharedMaterial = fullFriction;
        }
        else
        {
            rb.sharedMaterial = noFriction;
        }


    }

    //countdown til the end of a dash
    private float dashTimer = 0;
    //basically just a little thing to do the dash particles
    private bool lastDash = false;
    private void MovePlayer()
    {

        //prevents too fast
        if (rb.velocity.y > 50)
            rb.velocity = new Vector2(rb.velocity.x, 50);

        Jump();

        if (dashTimer <= 0 || !dashKey)
        {
            //allows you to extend a dash by jumping
            if (!isDashJumping)
            {
                isDashing = false;
                if (!dashKey)
                {
                    hasDashed = false;
                }
            }
        }


        if (dashKey && isGrounded && !hasDashed)
        {
            isDashing = true;
            hasDashed = true;
            dashTimer = 2;
        }

        //figure out move speed if dashing or whatever

        float finalMoveSpeed;

        if (isDashing)
        {
            jumpParticles.Play();
            //is this needed lol
            if (!isGrounded && !isDashJumping)
            {
                if (!isJumping)
                {
                    GroundSnap();
                }
                else
                {
                    isDashJumping = true;
                    dashBurstParticle.Stop();
                }
            }
            dashTimer -= .11f;
            //done to prevent some funky shit in the air
            //basically emulating megaman zero/x's dash jump where you only move if you input
            if (isDashJumping)
            {
                finalMoveSpeed = 10 * inputs.x;
            }
            else if (GetComponent<SpriteRenderer>().flipX == true)
            {
                finalMoveSpeed = -10;
                dashBurstParticle.transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {

                finalMoveSpeed = 10;
                dashBurstParticle.transform.localScale = new Vector3(1, 1, 1);
            }
        }
        else
        {
            finalMoveSpeed = inputs.x * moveSpeed;
            dashBurstParticle.Stop();
        }
        //just some funky shit for dash jumping wait why is this split up and not a single or what the fuck
        if (isDashJumping)
        {
            if (isGrounded || isSliding)
            {
                isDashJumping = false;
                isDashing = false;
                dashTimer = 0;
            }
        }

        //slope handling baby. from the youtube video earlier
        if (isGrounded && !onSlope)
        {
            rb.velocity = new Vector2(finalMoveSpeed, 0);
        }
        else if (isGrounded && onSlope && canWalkOnSlope && !isJumping)
        {
            rb.velocity = new Vector2(slopeNormalPerp.x * -finalMoveSpeed, slopeNormalPerp.y * -finalMoveSpeed);
        }
        else if (!isGrounded && !isWallJumping)
        {
            rb.velocity = new Vector2(finalMoveSpeed, rb.velocity.y);
        }
        //play the dash burst once per dash
        if(lastDash != isDashing)
        {
            if(isDashing)
            {
                dashBurstParticle.Play();
                Debug.Log("burst");
            }
            
            
        }
        lastDash = isDashing;

        coyoteBuffer -= 60 * Time.deltaTime;
        //if you fall off the ground like an idiot, attempt to snap back to the ground 
        if (lastOnGround != isGrounded)
        {
            if (!isGrounded)
            {

                if (!isJumping)
                {
                    GroundSnap();
                    coyoteBuffer = coyoteBufferLength;
                }
            }
            else
            {
                jumpBurstParticle.Play();
            }
        }
        lastOnGround = isGrounded;
    }
    //shoots a short ray and moves the player to the ground if it hits
    private Vector2 snapPos = Vector2.zero;
    private void GroundSnap()
    {
        Vector2 rayPosition = transform.position - new Vector3(0, .5f);
        RaycastHit2D hit = Physics2D.Raycast(rayPosition, Vector2.down, .2f, ground);

        if (hit)
        {
            snapPos = hit.point;
            transform.position = new Vector3(transform.position.x, hit.point.y + .5f, transform.position.z);
            Debug.Log("Snapped!");
        }
    }
    private void Jump()
    {
        bool leftWall = Physics2D.OverlapBox(leftTransform.position, new Vector2(.01f, .75f), 0f, ground);
        bool rightWall = Physics2D.OverlapBox(rightTransform.position, new Vector2(.01f, .75f), 0f, ground);
        if (isJumping && isGrounded)
            isJumping = false;

        //lower gravity at the height of a jump but only while holding the jump key like in celeste
        if (rb.velocity.y > -1 && rb.velocity.y < 1 && !isGrounded && !isSliding && jumpKey && hasJumped)
        {
            rb.gravityScale = jumpPeakGravityScale;
        }
        else
            rb.gravityScale = gravity;


        if ((jumpKey && (isGrounded) && !hasJumped) || (((jumpBuffer > 0 && isGrounded) || coyoteBuffer > 0) && jumpKey))
        {
            if (coyoteBuffer > 0)
            {
                //this part was inspired by this tweet here https://twitter.com/dhindes/status/1238348754790440961?s=20
                transform.position = new Vector3(transform.position.x, coyoteY, transform.position.z);
                rb.velocity = new Vector2(rb.velocity.x, 0);
            }
            coyoteBuffer = -100;
            rb.sharedMaterial = noFriction;
            jump = jumpForce;
            //this makes it function a tad better on slopes. there's a big of spaghetti rn but everything mostly works
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            startJump = true;
            hasJumped = true;
            isJumping = true;
            isGrounded = false;
        }
        //prevents holding jump
        else if (!jumpKey && (isGrounded || isSliding))
        {
            hasJumped = false;
            jump = 0;
        }
        //variable jump, cut the jump when you release the button
        if (!jumpKey && hasJumped && rb.velocity.y > 0 && !(isGrounded || isSliding))
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            jump = 0;
        }
        //if you are pushing into a wall in the air
        if (((leftWall && inputs.x < 0) || (rightWall && inputs.x > 0)) && !isGrounded)
        {
            if (rb.velocity.y < 0)
                isSliding = true;
        }
        else
        {
            isSliding = false;
        }

        if (leftWall || rightWall)
        {
            if (jumpKey && !hasJumped)
            {
                //walljumping stuff
                if (leftWall)
                {
                    //rb.AddRelativeForce(new Vector2((1 * moveSpeed) + jumpForce + 5, jumpForce));
                    //rb.velocity = new Vector2(1 * jumpForce, jumpForce * 1.2f);
                    rb.AddForce(new Vector2(jumpForce / 2, wallJumpForce), ForceMode2D.Force);
                }
                if (rightWall)
                {
                    //rb.AddRelativeForce(new Vector2((-1 * moveSpeed) + -jumpForce - 5, jumpForce));
                    //rb.velocity = new Vector2(-1 * jumpForce, jumpForce * 1.2f);
                    rb.AddForce(new Vector2(-jumpForce / 2, wallJumpForce), ForceMode2D.Force);
                }
                isWallJumping = true;
                isSliding = false;
                hasJumped = true;
                wallJumpCount = .15f;
            }
            else if (!jumpKey)
            {
                hasJumped = false;
            }
        }
        wallJumpCounter -= Time.deltaTime;

        jumpBuffer -= 60 * Time.deltaTime;





        //stupid but just for testing
        if (isSliding)
        {
            rb.velocity = Vector2.zero;
            rb.transform.Translate(new Vector2(0, -2.5f * Time.deltaTime));

            //move the particle to the correct wall for aesthetics
            if (leftWall)
            {
                slideParticles.transform.position = leftTransform.position;
            }
            else if (rightWall)
            {
                slideParticles.transform.position = rightTransform.position;
            }
            if (!slideParticles.isPlaying)
                slideParticles.Play();



            //rb.gravityScale = slideGravity;
            if (!jumpKey)
            {
                hasJumped = false;
                isWallJumping = false;
            }
        }
        else
        {
            slideParticles.Stop();
        }
        //funky particles fordash jumps
        if(isDashJumping)
        {
            jumpParticles.Play();
        }
        else
        {
            jumpParticles.Stop();
        }
    }
    private void ControlAnimations()
    {
        if (inputs.x > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            leftEar.GetComponent<SpriteRenderer>().flipX = false;
            rightEar.GetComponent<SpriteRenderer>().flipX = false;
        }
        if (inputs.x < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            leftEar.GetComponent<SpriteRenderer>().flipX = true;
            rightEar.GetComponent<SpriteRenderer>().flipX = true;
        }


        //this is a temporary thing and will lead to problems if i fuck up
        //make a proper state machine later
        rightEarTarget.transform.localPosition = new Vector3(.1f, .6f, 0);
        leftEarTarget.transform.localPosition = new Vector3(-.1f, .6f, 0);
        if (isGrounded)
        {
            if (isDashing)
            {
                animator.Play("AceyDashRough");
                rightEarTarget.transform.localPosition = new Vector3(.1f, .5f, 0);
                leftEarTarget.transform.localPosition = new Vector3(-.1f, .5f, 0);
            }
            else if (inputs.x != 0)
            {
                animator.Play("AceyWalkRough");
            }
            else
            {
                animator.Play("AceyIdleRough");
            }
        }
        else
        {
            if (rb.velocity.y < -2)
            {
                animator.Play("AceyJumpRoughDown");
            }
            else if (rb.velocity.y > -2 && rb.velocity.y < 2)
            {

                animator.Play("AceyJumpRoughMid");

            }
            else if (rb.velocity.y > 2)
            {
                rightEarTarget.transform.localPosition = new Vector3(.1f, .7f, 0);
                leftEarTarget.transform.localPosition = new Vector3(-.1f, .7f, 0);
                animator.Play("AceyJumpRoughUp");

            }


        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundTransform.position, groundCheckSize);
        Gizmos.DrawCube(leftTransform.position, new Vector2(.01f, .75f));
        Gizmos.DrawCube(rightTransform.position, new Vector2(.01f, .75f));
    }
}
