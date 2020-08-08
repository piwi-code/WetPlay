using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBoat : MonoBehaviour
{

    public Transform Motor;
    public float SteerPower = 500f;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Drag = 0.1f;


    private Rigidbody _rigidBody;
    private Quaternion _startRotation;


    void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _startRotation = Motor.localRotation;
    }

    void FixedUpdate()
    {
        //var forceDirection = transform.forward;

        // rotation
        var steer = -Input.GetAxis("Horizontal");
         _rigidBody.AddForceAtPosition(steer * transform.right * SteerPower / 100f, Motor.position);

        // forward
        var forward = Vector3.Scale(new Vector3(1,0,1), transform.forward);     // ensure only on water plane (not up / down)
        var forwardInput = Input.GetAxis("Vertical");
        if(forwardInput != 0)
            PhysicsHelper.ApplyForceToReachVelocity(_rigidBody, forward * MaxSpeed * forwardInput, Power);

    }
}
