using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Car settings")]
    public float driftFactor = 0.95f;
    public float accelerationFactor = 30.0f;
    public float turnFactor = 3.5f;
    public float maxSpeed = 20;

    [Header("Raycast settings")]
    public float raycastLength = 25f;
    public float raycastAngleFront = 9f;
    public float raycastAngleBack = 160f;
    public LayerMask raycastLayer;
    public bool drawRaycasts = false;
    public bool destructiveTracklimits = false;

    private float[] raycastDistances = new float[15];

    float accelerationInput = 0;
    float steeringInput = 0;
    float velocityVsUp = 0;

    private int nextCheckpointIndex = 0;

    [HideInInspector]
    public float nextCheckpointDistance = 0;
    [HideInInspector]
    public float totalSpeed = 0f;
    [HideInInspector]
    public int numberOfFrames = 0;
    [HideInInspector]
    public float totalDistanceTraveled = 0f;
    [HideInInspector]
    public int numberOfCheckpointsPassed = 0;
    [HideInInspector]
    public int numberOfTrackLimits = 0;

    private GameObject[] allCheckpoints;

    //Components
    Rigidbody2D carRigidbody2D;

    //Awake is called when the script instance is being loaded.
    void Awake()
    {
        carRigidbody2D = GetComponent<Rigidbody2D>();
        allCheckpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
    }

    //Frame-rate independent for physics calculations.
    void FixedUpdate()
    {
        UpdateStats();

        UpdateRays();

        ApplyEngineForce();

        KillOrthogonalVelocity();

        ApplySteering();
    }

    void ApplyEngineForce()
    {
        //Apply drag if there is no accelerationInput so the car stops when the player lets go of the accelerator
        if (accelerationInput == 0)
            carRigidbody2D.drag = Mathf.Lerp(carRigidbody2D.drag, 3.0f, Time.fixedDeltaTime * 3);
        else
            carRigidbody2D.drag = 0;

        //Caculate how much "forward" we are going in terms of the direction of our velocity
        velocityVsUp = Vector2.Dot(transform.up, carRigidbody2D.velocity);

        //Limit so we cannot go faster than the max speed in the "forward" direction
        if (velocityVsUp > maxSpeed && accelerationInput > 0)
            return;

        //Limit so we cannot go faster than the 50% of max speed in the "reverse" direction
        if (velocityVsUp < -maxSpeed * 0.5f && accelerationInput < 0)
            return;

        //Limit so we cannot go faster in any direction while accelerating
        if (carRigidbody2D.velocity.sqrMagnitude > maxSpeed * maxSpeed && accelerationInput > 0)
            return;

        //Create a force for the engine
        Vector2 engineForceVector = transform.up * accelerationInput * accelerationFactor;

        //Apply force and pushes the car forward
        carRigidbody2D.AddForce(engineForceVector, ForceMode2D.Force);
    }

    void ApplySteering()
    {
        // Limit the car's ability to turn when moving slowly
        float minSpeedBeforeAllowTurningFactor = (GetVelocityMagnitude() / 2);
        minSpeedBeforeAllowTurningFactor = Mathf.Clamp01(minSpeedBeforeAllowTurningFactor);

        // Calculate the new rotation angle based on input
        float newRotationAngle = transform.rotation.eulerAngles.z - (steeringInput * turnFactor * minSpeedBeforeAllowTurningFactor);

        // Apply steering by rotating the car object
        carRigidbody2D.MoveRotation(newRotationAngle);
    }



    void KillOrthogonalVelocity()
    {
        //Get forward and right velocity of the car
        Vector2 forwardVelocity = transform.up * Vector2.Dot(carRigidbody2D.velocity, transform.up);
        Vector2 rightVelocity = transform.right * Vector2.Dot(carRigidbody2D.velocity, transform.right);

        //Kill the orthogonal velocity (side velocity) based on how much the car should drift. 
        carRigidbody2D.velocity = forwardVelocity + rightVelocity * driftFactor;
    }

    float GetLateralVelocity()
    {
        //Returns how how fast the car is moving sideways. 
        return Vector2.Dot(transform.right, carRigidbody2D.velocity);
    }

    public bool IsTireScreeching(out float lateralVelocity, out bool isBraking)
    {
        lateralVelocity = GetLateralVelocity();
        isBraking = false;

        //Check if we are moving forward and if the player is hitting the brakes. In that case the tires should screech.
        if (accelerationInput < 0 && velocityVsUp > 0)
        {
            isBraking = true;
            return true;
        }

        //If we have a lot of side movement then the tires should be screeching
        if (Mathf.Abs(GetLateralVelocity()) > 4.0f)
            return true;

        return false;
    }

    public void SetInputVector(Vector2 inputVector)
    {
        steeringInput = inputVector.x;
        accelerationInput = inputVector.y;
    }

    void UpdateRays()
    {
        // Raycast 9 rays in front of the car
        for (int i = 0; i < 9; i++)
        {
            // Calculate the direction of the ray based on the current rotation
            float angle = (float)(i - 4.5) * raycastAngleFront; // Adjust the angle as needed
            Vector2 direction = Quaternion.Euler(0, 0, angle) * transform.up;

            // Perform the raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastLength, raycastLayer);

            // Store the distance to the hit point or set to the maximum length if no hit
            raycastDistances[i] = hit ? hit.distance : raycastLength;

            // Optionally, visualize the raycast lines in the Unity scene view for debugging
            if (drawRaycasts)
                Debug.DrawRay(transform.position, direction * raycastDistances[i], Color.red);
        }

        // Raycast 6 rays to the sides of the car
        for (int i = 0; i < 6; i++)
        {
            // Calculate the direction of the ray based on the current rotation
            float angle = (float)(i - 2.5f) * raycastAngleBack; // Adjust the angle as needed
            Vector2 direction = Quaternion.Euler(0, 0, angle) * -transform.up;

            // Perform the raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastLength, raycastLayer);

            // Store the distance to the hit point or set to the maximum length if no hit
            raycastDistances[i + 9] = hit ? hit.distance : raycastLength;

            // Optionally, visualize the raycast lines in the Unity scene view for debugging
            if (drawRaycasts)
                Debug.DrawRay(transform.position, direction * raycastDistances[i + 9], Color.red);
        }
    }

    public float GetVelocityMagnitude()
    {
        return carRigidbody2D.velocity.magnitude;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Checkpoint") && allCheckpoints[nextCheckpointIndex] == other.gameObject)
        {
            numberOfCheckpointsPassed++;
            nextCheckpointIndex++;
            if (nextCheckpointIndex >= allCheckpoints.Length)
            {
                nextCheckpointIndex = 0;
            }
        } else if (other.CompareTag("InsideTrackLimit") || other.CompareTag("OutsideTrackLimit"))
        {
            numberOfTrackLimits++;
            if (destructiveTracklimits)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void UpdateStats()
    {
        // Inside FixedUpdate method
        // Calculate the distance traveled in this frame
        float distanceThisFrame = GetVelocityMagnitude() * Time.fixedDeltaTime;

        // Add this frame's speed to the total speed
        totalSpeed += GetVelocityMagnitude();

        // Add the distance traveled in this frame to the total distance
        totalDistanceTraveled += distanceThisFrame;

        // Increment the frame count
        numberOfFrames++;

        // Calculate the distance to the next checkpoint
        nextCheckpointDistance = Vector2.Distance(transform.position, allCheckpoints[nextCheckpointIndex].transform.position);
    }

    public float GetAverageSpeed()
    {
        return totalSpeed / numberOfFrames;
    }
}
