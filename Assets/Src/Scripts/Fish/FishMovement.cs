using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Velocidad y Comportamiento")]
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float minIdleTime = 0.5f;
    [SerializeField] private float maxIdleTime = 2.0f;
    [SerializeField] private float targetReachThreshold = 0.2f;
    [SerializeField] private float maxTimePerTarget = 5.0f;

    [SerializeField] private float paddingX = 1.0f;
    [SerializeField] private float paddingY = 0.8f;

    [SerializeField] private bool restrictToWater = true;
    [SerializeField] private float waterMinY = -4.2f;
    [SerializeField] private float waterMaxY = -1.2f;

    [SerializeField] private bool enableWiggle = true;
    [SerializeField] private float wiggleFrequency = 4f;
    [SerializeField] private float wiggleAmplitude = 0.15f;

    private Camera mainCamera;
    private Vector2 targetPosition;
    private bool isWaiting;
    private float waitTimer;
    private float targetTimer;
    private Vector3 initialScale;

    private void Awake()
    {
        mainCamera = Camera.main;
        initialScale = transform.localScale;
    }

    private void Start()
    {
        PickNewTargetPosition();
    }

    private void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewTargetPosition();
            }
            return;
        }

        targetTimer += Time.deltaTime;

        Vector2 currentPos = transform.position;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPosition, speed * Time.deltaTime);

        float moveX = newPos.x - currentPos.x;
        UpdateFacing(moveX);

        float wiggleOffset = 0f;
        if (enableWiggle)
        {
            wiggleOffset = Mathf.Sin(Time.time * wiggleFrequency) * wiggleAmplitude * Time.deltaTime;
        }

        Vector3 finalPosition = new Vector3(newPos.x, newPos.y + wiggleOffset, transform.position.z);

        GetBounds(out float minX, out float maxX, out float minY, out float maxY);
        finalPosition.x = Mathf.Clamp(finalPosition.x, minX, maxX);
        finalPosition.y = Mathf.Clamp(finalPosition.y, minY, maxY);

        transform.position = finalPosition;

        if (Vector2.Distance(transform.position, targetPosition) <= targetReachThreshold || targetTimer >= maxTimePerTarget)
        {
            isWaiting = true;
            waitTimer = Random.Range(minIdleTime, maxIdleTime);
        }
    }

    private void PickNewTargetPosition()
    {
        targetTimer = 0f;
        GetBounds(out float minX, out float maxX, out float minY, out float maxY);

        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        targetPosition = new Vector2(randomX, randomY);
    }

    private void GetBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.orthographic)
        {
            float vertExtent = mainCamera.orthographicSize;
            float horizExtent = vertExtent * mainCamera.aspect;
            Vector3 camPos = mainCamera.transform.position;

            minX = camPos.x - horizExtent + paddingX;
            maxX = camPos.x + horizExtent - paddingX;

            if (restrictToWater)
            {
                minY = waterMinY;
                maxY = waterMaxY;
            }
            else
            {
                minY = camPos.y - vertExtent + paddingY;
                maxY = camPos.y + vertExtent - paddingY;
            }
        }
        else
        {
            minX = -6f;
            maxX = 6f;
            minY = restrictToWater ? waterMinY : -4f;
            maxY = restrictToWater ? waterMaxY : 4f;
        }
    }

    private void UpdateFacing(float directionX)
    {
        if (Mathf.Abs(directionX) > 0.001f)
        {
            float baseScaleX = Mathf.Abs(initialScale.x);

            float newScaleX = directionX > 0 ? -baseScaleX : baseScaleX;

            transform.localScale = new Vector3(newScaleX, initialScale.y, initialScale.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        GetBounds(out float minX, out float maxX, out float minY, out float maxY);

        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
        Gizmos.DrawWireCube(center, size);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(new Vector3(targetPosition.x, targetPosition.y, transform.position.z), 0.15f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}