using System;
using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private FixedJoystick Joystick;
    private Animator Animator;
    public bool isMoving;
    private Vector3 input;
    private Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Animator = GetComponentInChildren<Animator>();
    }
    
    void Update()
    {
        input.x = Joystick.Horizontal;
        input.z = Joystick.Vertical;
        if (input.x != 0 || input.z != 0)
        {
            isMoving = true;
            Animator.SetBool("isMoving", true);
            Vector3 relivePos = (new Vector3(input.x, 0, input.z)) ;                                 
            Quaternion rotation = Quaternion.LookRotation(relivePos);                                
            Quaternion current = transform.localRotation;                                            
            transform.localRotation = Quaternion.Slerp(current, rotation, 10f*Time.deltaTime);
        }
        else
        {
            isMoving = false;
            Animator.SetBool("isMoving", false);
        }
        

    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + input * speed * Time.fixedDeltaTime);   

    }
}
