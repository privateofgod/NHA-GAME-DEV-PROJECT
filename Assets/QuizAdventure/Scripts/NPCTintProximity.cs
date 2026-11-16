using UnityEngine;
using UnityEngine.AI;

public class NPCTintProximity : MonoBehaviour
{
    [Header("References")]
    public GameObject tintCanvas;
    public Transform playerTransform;
    
    [Header("Interaction")]
    public float interactionDistance = 5f;
    
    private bool canvasVisible = false;
    private MovableObjectMotor objectMotor;
    private NavMeshAgent navAgent;
    private Canvas worldCanvas;
    
    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        objectMotor = GetComponent<MovableObjectMotor>();
        navAgent = GetComponent<NavMeshAgent>();
        
        if (tintCanvas != null)
        {
            worldCanvas = tintCanvas.GetComponent<Canvas>();
            if (worldCanvas != null && worldCanvas.worldCamera == null)
            {
                worldCanvas.worldCamera = Camera.main;
            }
            tintCanvas.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null || tintCanvas == null)
            return;
        
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool shouldShow = dist <= interactionDistance;
        
        if (shouldShow != canvasVisible)
        {
            canvasVisible = shouldShow;
            tintCanvas.SetActive(canvasVisible);

            // If NPC is wandering, pause it during interaction so the player can drag the slider comfortably
            if (canvasVisible)
            {
                if (objectMotor != null) objectMotor.Stop();
                else if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh) navAgent.isStopped = true;
            }
            else
            {
                if (objectMotor != null) objectMotor.Go();
                else if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh) navAgent.isStopped = false;
            }
        }

        // Keep canvas facing the camera
        if (canvasVisible && Camera.main != null && tintCanvas != null)
        {
            if (worldCanvas != null && worldCanvas.worldCamera == null)
                worldCanvas.worldCamera = Camera.main;

            Vector3 dir = tintCanvas.transform.position - Camera.main.transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                tintCanvas.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
