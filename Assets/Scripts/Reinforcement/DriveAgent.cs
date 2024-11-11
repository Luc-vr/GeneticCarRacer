using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DriveAgent : Agent
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

    [Header("Spawnpoint")]
    public Transform spawnPoint;

    [Header("Learning settings")]
    public bool stopGenAfterAmountOfLaps = true;
    public int amountOfLaps = 3;
    public int checkpointReward = 1;
    public int trackLimitPunishment = -2;


    private float[] raycastDistances = new float[15];

    private float accelerationInput = 0;
    private float steeringInput = 0;
    private float velocityVsUp = 0;

    private int nextCheckpointIndex = 0;
    private int lapsCompleted = 0;

    private GameObject[] allCheckpoints;

    //Components
    Rigidbody2D carRigidbody2D;

    //Awake is called when the script instance is being loaded.
    void Awake()
    {
        carRigidbody2D = GetComponent<Rigidbody2D>();
        allCheckpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float turn = actions.ContinuousActions[0];
        float forward = actions.ContinuousActions[1];
        
        steeringInput = turn;
        accelerationInput = forward;

        UpdateRays();

        ApplyEngineForce();

        KillOrthogonalVelocity();

        ApplySteering();
        
        // Punish the agent a small amount every step to encourage it to finish the track quickly
        //AddReward(-0.01f);

        // Punish when the car is going in reverse
        //if (velocityVsUp < 0)
        //    AddReward(-0.1f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add all raycast distances and the car's velocity as observations
        sensor.AddObservation(raycastDistances);
        sensor.AddObservation(carRigidbody2D.velocity);
    }

    public override void OnEpisodeBegin()
    {
        // Reset the car's position and rotation to the spawn point
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        // Reset the car's velocity
        carRigidbody2D.velocity = new Vector2(0, 0);

        // Reset the input values
        steeringInput = 0;
        accelerationInput = 0;

        // Reset the checkpoint index
        nextCheckpointIndex = 0;

        // Reset the lap counter
        lapsCompleted = 0;
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
            // Add a reward for passing a checkpoint
            AddReward(checkpointReward);

            //numberOfCheckpointsPassed++;
            nextCheckpointIndex++;
            if (nextCheckpointIndex >= allCheckpoints.Length)
            {
                nextCheckpointIndex = 0;
                lapsCompleted++;
                if (stopGenAfterAmountOfLaps && lapsCompleted >= amountOfLaps)
                {
                    EpisodeInterrupted();
                }
            }
        } else if (other.CompareTag("InsideTrackLimit") || other.CompareTag("OutsideTrackLimit"))
        {
            // Punish the agent for going outside the track
            AddReward(trackLimitPunishment);

            //numberOfTrackLimits++;
            if (destructiveTracklimits)
            {
                EndEpisode();
            }
        }
    }
}
