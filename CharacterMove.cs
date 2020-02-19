using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    #region Deklaracje zmiennych
    private string MoveInputAxis = "Vertical";
    private string RotateInputAxis = "Horizontal";

    public Camera thirdPersonCamera;
    private Rigidbody rb;
    private Animator anim;
    private float rotationSpeed = 3f;
    private float horizontalMoveInput = 0f;
    private float verticalMoveInput = 0f;
    float jumpForce = 150f;
    float maxSpeed = 9f;
    float moveForwardSpeed = 12f;
    float moveBackwardOrSidewaysSpeed = 10f;
    float bouncingOffEnemy = 15f;
    public bool isGrounded = true;
    bool isFalling = false;
    float oldHeightPos;

    //Enum wykożystywany do ustawiania zmiennej odpowiadającej za aktualnie odgrywaną animację postaci
    enum actionEnum
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        WalkBackward = 3,
        WalkLeft = 4,
        WalkRight = 5,
        WalkForwardLeft = 6,
        WalkForwardRight = 7,
        WalkBackwardLeft = 8,
        WalkBackwardRight = 9,
        Jump = 10,
        Fall = 11
    }
    #endregion

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        oldHeightPos = transform.position.y;
    }

    //Wykorzystuję FixedUpdate() zamiast Update() ponieważ używam Rigidbody a nie CharacterController do obliczania fizyki postaci
    void FixedUpdate()
    {
        horizontalMoveInput = Input.GetAxis(MoveInputAxis);
        verticalMoveInput = Input.GetAxis(RotateInputAxis);
        Move(horizontalMoveInput, verticalMoveInput);
        Turn();

    }

    //Funkcja odpowiedzialna za poruszanie się postaci i ustawianie animacji
    private void Move(float horizontalMoveInput, float verticalMoveInput)
    {
        if (isGrounded)
        {
            anim.SetInteger("Action", (int)actionEnum.Idle);
            //Postać porusza się w dwóch osiach
            if (horizontalMoveInput != 0 && verticalMoveInput != 0)
            {
                rb.AddForce(transform.forward * horizontalMoveInput * moveForwardSpeed * 0.6F * Time.deltaTime, ForceMode.VelocityChange);
                rb.AddForce(transform.right * verticalMoveInput * moveBackwardOrSidewaysSpeed * 0.6F * Time.deltaTime, ForceMode.VelocityChange);

                //ustawianie animacji ruchu zgodnie z kierunkiem poruszania się postaci
                if (horizontalMoveInput > 0 && verticalMoveInput > 0)
                {
                    anim.SetInteger("Action", (int)actionEnum.WalkForwardRight);
                }
                if (horizontalMoveInput < 0 && verticalMoveInput > 0)
                {
                    anim.SetInteger("Action", (int)actionEnum.WalkBackwardRight);
                }
                if (horizontalMoveInput > 0 && verticalMoveInput < 0)
                {
                    anim.SetInteger("Action", (int)actionEnum.WalkForwardLeft);
                }
                if (horizontalMoveInput < 0 && verticalMoveInput < 0)
                {
                    anim.SetInteger("Action", (int)actionEnum.WalkBackwardLeft);
                }
            }
            //Postać porusza się w jednej osi
            else if (horizontalMoveInput < 0)
            {
                rb.AddForce(transform.forward * horizontalMoveInput * moveBackwardOrSidewaysSpeed * Time.deltaTime, ForceMode.VelocityChange);
                anim.SetInteger("Action", (int)actionEnum.WalkBackward);
            }
            else if (horizontalMoveInput > 0)
            {
                rb.AddForce(transform.forward * horizontalMoveInput * moveForwardSpeed * Time.deltaTime, ForceMode.VelocityChange);
                anim.SetInteger("Action",(int)actionEnum.Walk);
            }
            //Postać porusza się w lewo lub prawo
            else if (verticalMoveInput != 0)
            {
                rb.AddForce(transform.right * verticalMoveInput * moveBackwardOrSidewaysSpeed * Time.deltaTime, ForceMode.VelocityChange);
                if (verticalMoveInput > 0) 
                {
                    anim.SetInteger("Action", (int)actionEnum.WalkRight);
                }
                else if (verticalMoveInput < 0)
                {
                    anim.SetInteger("Action", (int)actionEnum.WalkLeft);
                }
            }
            //Ograniczenie maksymalnej prędkości poruszania się postaci
            if(rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
            Jump();
        }
        else if (!isGrounded)
        {
            //Możliwość kontroli postaci gdy znajduje się w powietrzu
            rb.AddForce(transform.forward * horizontalMoveInput * moveBackwardOrSidewaysSpeed * 0.6F * Time.deltaTime, ForceMode.VelocityChange);
            rb.AddForce(transform.right * verticalMoveInput * moveBackwardOrSidewaysSpeed * 0.6F * Time.deltaTime, ForceMode.VelocityChange);

            //Sprawdzenie czy postać spada
            if (transform.position.y < oldHeightPos)
            {
                anim.SetInteger("Action", (int)actionEnum.Fall);
                isFalling = true;
            }
            isFalling = false;
            oldHeightPos = transform.position.y;
        }
        
    }

    // Funkcaja odpowiedzialna za stopniowe obracanie się postaci wraz z ruchem kamery sterowanej myszką
    private void Turn()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, new Quaternion(0, thirdPersonCamera.transform.rotation.y,0, thirdPersonCamera.transform.rotation.w) , rotationSpeed * Time.deltaTime);
    }

    #region Kolizje
    void OnCollisionEnter(Collision theCollision)
    {
        //Kolizja z podłożem
        if (theCollision.gameObject.CompareTag("Floor"))
        {
            isGrounded = true;
        }
        //Kolizja z przeciwnikiem, póki co naskakując od góry możemy się od nich odbijać
        if (theCollision.gameObject.CompareTag("Enemy"))
        {
            Vector3 localPoint = theCollision.transform.InverseTransformPoint(transform.position);
            Vector3 localDir = localPoint.normalized;
            float upDot = Vector3.Dot(localDir, Vector3.up);
            if (upDot > 0)
            {
                rb.AddForce(transform.up  * bouncingOffEnemy, ForceMode.VelocityChange);
            }
        }
    }

    void OnCollisionExit(Collision theCollision)
    {
        //Opuszczenie kolizji z podłożem
        if (theCollision.gameObject.CompareTag("Floor"))
        {
            isGrounded = false;
        }
    }
    #endregion

    //Skakanie
    private void Jump()
    {
        if (Input.GetKey(KeyCode.Space))
        {
                rb.AddForce(new Vector3(0f, jumpForce * Time.deltaTime, 0),ForceMode.VelocityChange);
                anim.SetInteger("Action", (int)actionEnum.Jump);
        }
    }
}
