using Elendow.SpritedowAnimator;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Experimental.Rendering.Universal;

[RequireComponent(typeof(Controller2D))]
public class PlayerController : MonoBehaviour
{
    //note to self, add the ears to this script to be able to move them if acey ever needs to teleport

    Controller2D controller;

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
    public float groundSnapLength = 1f;

    [Header("Player Variables")]
    private float jump = 0;
    public bool isDashing = false;
    private bool isDashJumping = false;
    public bool hasDashed = false;
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

    public float swingFirstLength = 1;
    public float swingTimer = 0;
    public float swingFirstCancel = .8f;

    public float jumpVelocity = 0;
    public float timeToJumpApex = .5f;
    public float jumpHeight = 1.25f;


    //this should be able to do sword buffering
    private bool hasSwung = false;
    public bool isSwinging = false;
    //used for the sword script to reference
    public bool flipX = false;

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
    private bool gunKey = false;
    private bool swordKey = false;
    public Rigidbody2D rb;


    public Transform leftTransform;
    public Transform rightTransform;
    public Transform groundTransform;



    public float groundCheckSize = .25f;

    public float boxSizeX = .95f;

    private bool lastOnGround = false;

    public LayerMask ground;

    public AfterImageController testController;
    public GameObject swordPrefab;

    Vector2 velocity;

    public TextMeshProUGUI text;

    float velocityXSmoothing;

    float currentFriction = 0;
    float friction = .02f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<SpriteAnimator>();
        controller = GetComponent<Controller2D>();

        gravity = -(jumpHeight * 2) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;

        leftEar.transform.parent = null;
        rightEar.transform.parent = null;

        text = FindObjectOfType<TextMeshProUGUI>();
        leftEarSprite = leftEar.GetComponent<SpriteRenderer>();
        rightEarSprite = rightEar.GetComponent<SpriteRenderer>();
        playerSprite = GetComponent<SpriteRenderer>();
    }
    //these are here to drastically cut down on the amount of GetComponent calls 
    SpriteRenderer leftEarSprite;
    SpriteRenderer rightEarSprite;
    SpriteRenderer playerSprite;
    private void FixedUpdate()
    {
        frames++;
        //this is done in fixed update to prevent it making too many afterimages when in full speed
        if (isDashing && frames % 2 == 0)
        {
            Color dashColor = new Color(.25f, 1, 1, 1);
            //creates after images
            Instantiate(testController).CreateAfterImage(dashColor, 10f, new Vector3(transform.position.x, transform.position.y, transform.position.z + 1),
            animator.SpriteRenderer.sprite,playerSprite.flipX);
            Instantiate(testController).CreateAfterImage(dashColor, 10f, new Vector3(rightEar.transform.position.x, rightEar.transform.position.y, transform.position.z + 1),
            rightEarSprite.sprite, rightEarSprite.flipX);
            Instantiate(testController).CreateAfterImage(dashColor, 10f, new Vector3(leftEar.transform.position.x, leftEar.transform.position.y, transform.position.z + 1),
            leftEarSprite.sprite, leftEarSprite.flipX);
        }
    }
    private void Update()
    {
        text.text = (1 / Time.deltaTime).ToString() ;

        //debug thing,remove later please
        if (Input.GetKeyDown(KeyCode.B))
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
        swordKey = Input.GetKey(KeyCode.C);


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

        if(inputs.x != 0)
        {
            controller.collisions.faceDir =(int)Mathf.Sign(inputs.x);
        }



        if (controller.collisions.below)
            currentFriction = 0;
        else
            currentFriction = friction;
        float targetX = inputs.x * moveSpeed;
        //adds a slight amount of momentum to movement
        velocity.x = Mathf.SmoothDamp(velocity.x, targetX, ref velocityXSmoothing, currentFriction);

        

        //the player will reset to this position upon attempting a coyote jump
        isGrounded = controller.collisions.below;

        if (isGrounded)
        {
            coyoteY = transform.position.y;
        }
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;

        MovePlayer();

        AttackHandler();

        ControlAnimations();




        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        

    }

   


    private void AttackHandler()
    {
        //this should probably be in fixed update but this is ok
        swingTimer -= Time.deltaTime;
        if (swordKey && !hasSwung && !isSwinging && swingTimer < 0)
        {
            hasSwung = true;
            isSwinging = true;
            Instantiate(swordPrefab).GetComponent<SwordHitBox>().Init(.2f, Vector2.one, new Vector2(1, 0), this.gameObject);
            swingTimer = swingFirstLength;
        }
        //allows you to cancel the swing by walkin
        if (swingTimer < swingFirstCancel && (inputs.x != 0 || dashKey || jumpKey))
        {
            isSwinging = false;
        }
        //prevents you from walking, jumping, or dashing while swinging
        //this should probably be redone a touch but it works for now
        if (isSwinging)
        {
            inputs = Vector2.zero;
            dashKey = false;
            jumpKey = false;
        }
        if (!isSwinging && !swordKey)
        {
            hasSwung = false;

        }

        //!isSwinging  && !swordKey &&
        if (swingTimer < 0)
        {

            isSwinging = false;
        }
    }

    private float wallJumpCount = 0;
    private int frames = 0;



    //countdown til the end of a dash
    public float dashTimer = 0;
    //basically just a little thing to do the dash particles
    private bool lastDash = false;
    private void MovePlayer()
    {

        Jump();

        if (isDashJumping)
        {
            if (isGrounded || isSliding)
            {
                isDashJumping = false;
                isDashing = false;
                dashTimer = 0;
            }
        }

        if (dashTimer <= 0 || !dashKey)
        {
            //allows you to extend a dash by jumping
            if (!isDashJumping)
            {
                isDashing = false;
                if (!dashKey)
                {
                    hasDashed = false;
                    dashTimer = -1;
                }
            }
        }


        if (dashKey && isGrounded && !hasDashed && dashTimer < 0)
        {
            Debug.Log("dash start baby");
            isDashing = true;
            hasDashed = true;
            dashTimer = .05f;
        }

        dashTimer -= .11f * Time.deltaTime;
        if (isDashing)
        {
            jumpParticles.Play();
            //is this needed lol

            if (isJumping)
            {
                //just to make sure you can jump from a slope
                if (controller.collisions.fallen && !isGrounded)
                {
                    controller.collisions.below = false;
                }
                isDashJumping = true;
                dashBurstParticle.Stop();
            }

            //done to prevent some funky shit in the air
            //basically emulating megaman zero/x's dash jump where you only move if you input
            float flip = (flipX) ? -1 : 1;
            if (isDashJumping)
            {
                velocity.x = 10 * inputs.x;
            }
            else
            {
                velocity.x = 10 * flip;
                dashBurstParticle.transform.localScale = new Vector3(flip, 1, 1);
            }
        }
        else
        {
            velocity.x = inputs.x * moveSpeed;
            dashBurstParticle.Stop();
        }
        //just some funky shit for dash jumping wait why is this split up and not a single or what the fuck



        //play the dash burst once per dash
        //wait does this fucking line make sense anymore
        if (lastDash != isDashing)
        {
            if (isDashing)
            {
                dashBurstParticle.Play();
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
                    coyoteBuffer = coyoteBufferLength;
                }
            }
            else
            {
                //if(coyoteBuffer < 0)
                jumpBurstParticle.Play();
            }
        }
        lastOnGround = isGrounded;
    }

    private void Jump()
    {
        bool leftWall = controller.collisions.left;
        bool rightWall = controller.collisions.right;
        if (isJumping && isGrounded)
            isJumping = false;

        //lower gravity at the height of a jump but only while holding the jump key like in celeste
        if (velocity.y > -1 && velocity.y < 1 && !isGrounded && !isSliding && jumpKey && hasJumped)
        {
            //rb.gravityScale = jumpPeakGravityScale;
        }
        else
        // rb.gravityScale = gravity;


        if ((jumpKey && (isGrounded) && !hasJumped) || (((jumpBuffer > 0 && isGrounded) || coyoteBuffer > 0) && jumpKey))
        {
            if (coyoteBuffer > 0)
            {
                //this part was inspired by this tweet here https://twitter.com/dhindes/status/1238348754790440961?s=20
                transform.position = new Vector3(transform.position.x, coyoteY, transform.position.z);
            }
            coyoteBuffer = -100;
            jump = jumpForce;

            if(controller.collisions.descendingSlope)
            {
                controller.collisions.descendingSlope = false;
                controller.collisions.below = false;
            }    


            //this makes it function a tad better on slopes. there's a big of spaghetti rn but everything mostly works
            velocity.y = jumpVelocity;
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
        if (!jumpKey && hasJumped && velocity.y > 0 && !(isGrounded || isSliding))
        {
            velocity = new Vector2(velocity.x, 0);
            jump = 0;
        }
        //if you are pushing into a wall in the air
        if (((leftWall && inputs.x < 0) || (rightWall && inputs.x > 0)) && !isGrounded)
        {
            if (velocity.y < 0)
                isSliding = true;
        }
        else
        {
            isSliding = false;
        }
        /*
        if (leftWall || rightWall)
        {
            if (jumpKey && !hasJumped)
            {
                //walljumping stuff
                if (leftWall)
                {
                    //rb.AddRelativeForce(new Vector2((1 * moveSpeed) + jumpForce + 5, jumpForce));
                    //rb.velocity = new Vector2(1 * jumpForce, jumpForce * 1.2f);
                    //rb.AddForce(new Vector2(jumpForce / 2, wallJumpForce), ForceMode2D.Force);
                }
                if (rightWall)
                {
                    //rb.AddRelativeForce(new Vector2((-1 * moveSpeed) + -jumpForce - 5, jumpForce));
                    //rb.velocity = new Vector2(-1 * jumpForce, jumpForce * 1.2f);
                    //rb.AddForce(new Vector2(-jumpForce / 2, wallJumpForce), ForceMode2D.Force);
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

        
        */



        jumpBuffer -= Time.deltaTime;
        //stupid but just for testing
        if (isSliding)
        {
            //rb.velocity = Vector2.zero;
            //rb.transform.Translate(new Vector2(0, -2.5f * Time.deltaTime));

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
        if (isDashJumping)
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
        //this is how it be for now
        //had to make a simple custom shader graph to make normals flip correctly, which is what the setfloat stuff is here

        string animName = "";
        if (inputs.x > 0)
        {
            flipX = false;
            //GetComponent<SpriteRenderer>().flipX = false;
            //GetComponent<SpriteRenderer>().material.SetFloat("FlipX", 0);
            leftEarSprite.flipX = false;
            leftEarSprite.material.SetFloat("FlipX", 0);
            rightEarSprite.flipX = false;
            rightEarSprite.material.SetFloat("FlipX", 0);
        }
        if (inputs.x < 0)
        {
            flipX = true;
            //GetComponent<SpriteRenderer>().flipX = true;
            //GetComponent<SpriteRenderer>().material.SetFloat("FlipX", 1);
            leftEarSprite.flipX = true;
            leftEarSprite.material.SetFloat("FlipX", 1);
            rightEarSprite.flipX = true;
            rightEarSprite.material.SetFloat("FlipX", 1);
        }


        //this is a temporary thing and will lead to problems if i fuck up
        //make a proper state machine later
        rightEarTarget.transform.localPosition = new Vector3(.1f, .6f, 0);
        leftEarTarget.transform.localPosition = new Vector3(-.1f, .6f, 0);
        if (isGrounded)
        {
            if (isDashing)
            {
                //animator.Play("AceyDashRough");
                animName = "AceyDashRough";
                //rightEarTarget.transform.localPosition = new Vector3(.1f, .5f, 0);
                //leftEarTarget.transform.localPosition = new Vector3(-.1f, .5f, 0);
            }
            else if (inputs.x != 0)
            {
                //animator.Play("AceyWalkRough");
                animName = "AceyWalkRough";
            }
            else
            {
                //animator.Play("AceyIdleRough");
                animName = "AceyIdleRough";
            }
        }
        else
        {
            if (velocity.y < -2)
            {
                //animator.Play("AceyJumpRoughDown");
                animName = "AceyJumpRoughDown";
            }
            else if (velocity.y > -2 && velocity.y < 2)
            {

                //animator.Play("AceyJumpRoughMid");
                animName = "AceyJumpRoughMid";

            }
            else if (velocity.y > 2)
            {
                rightEarTarget.transform.localPosition = new Vector3(.1f, .7f, 0);
                leftEarTarget.transform.localPosition = new Vector3(-.1f, .7f, 0);
                //animator.Play("AceyJumpRoughUp");
                animName = "AceyJumpRoughUp";

            }


        }

        if (isSwinging)
        {
            animName = "AceySwordSlashFirst";
        }

        //a fallback thing to make sure that if i don't have a flip sprite it will play the non flipped version
        if (flipX)
        {
            bool hasAnim = false;
            //check if the animation is found
            foreach (SpriteAnimation anim in animator.animations)
            {
                if (anim.name == animName + "Flip")
                {
                    hasAnim = true;
                    break;
                }
            }


            if (!hasAnim)
            {
                animator.Play(animName);
                playerSprite.flipX = true;
                playerSprite.material.SetFloat("FlipX", 1);
            }
            else
            {
                animator.Play(animName + "Flip");
                playerSprite.flipX = false;
                playerSprite.material.SetFloat("FlipX", 0);
            }
        }
        else
        {
            animator.Play(animName);
            playerSprite.flipX = false;
            playerSprite.material.SetFloat("FlipX", 0);
        }


    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundTransform.position, groundCheckSize);
        Gizmos.DrawCube(leftTransform.position, new Vector2(.01f, .75f));
        Gizmos.DrawCube(rightTransform.position, new Vector2(.01f, .75f));
    }
}
