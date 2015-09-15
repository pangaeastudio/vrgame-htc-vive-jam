using UnityEngine;
using System.Collections;
using RootMotion.Demos;

public class MoveController : MonoBehaviour
{
    NavMeshAgent navAgent;
    int floorMask;                      //   A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    float camRayLength = 100f;          // The length of the ray from the camera into the scene.

    Vector3 targetPosition;
    Animator animator;
    bool isNavigating;

    void Awake()
    {
        // Create a layer mask for the floor layer.
        floorMask = LayerMask.GetMask("Water");

        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isNavigating)
        {
            if ((transform.position - targetPosition).magnitude < navAgent.stoppingDistance + 0.1)
            {
                isNavigating = false;
                //navAgent.Stop();
                animator.SetBool("IsWalk", false);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Create a RaycastHit variable to store information about what was hit by the ray.
            RaycastHit floorHit;

            // Perform the raycast and if it hits something on the floor layer...
            if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
            {
                targetPosition = floorHit.point;
                isNavigating = true;
                navAgent.SetDestination(floorHit.point);
                //// Create a vector from the player to the point on the floor the raycast from the mouse hit.
                //Vector3 playerToMouse = floorHit.point - transform.position;

                //// Ensure the vector is entirely along the floor plane.
                //playerToMouse.y = 0f;

                //// Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
                //Quaternion newRotatation = Quaternion.LookRotation(playerToMouse);

                //// Set the player's rotation to this new rotation.
                //playerRigidbody.MoveRotation(newRotatation);

                animator.SetBool("IsWalk", true);
            }
        }
    }


}
