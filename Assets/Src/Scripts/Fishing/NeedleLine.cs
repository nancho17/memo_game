using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla la simulación física de la línea de pesca y el efecto de lanzamiento (casting)
/// del anzuelo utilizando el nuevo Input System (InputSystem_Actions).
/// </summary>
public class NeedleLine : MonoBehaviour
{
    public enum FishingState
    {
        IdleAtRod,
        Casting,
        FloatingInWater,
        Reeling
    }

    [Header("Referencias Principales")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform pivot;
    [SerializeField] private Transform targetHook;
    [Tooltip("Posición opcional de descanso para el anzuelo en la caña (si es null, usa el pivot)")]
    [SerializeField] private Transform hookRestPoint;

    [Header("Input System (InputSystem_Actions)")]
    [Tooltip("Acción para lanzar el anzuelo (ej. Attack o Jump en InputSystem_Actions)")]
    [SerializeField] private InputActionReference castAction;

    [Tooltip("Acción para recoger el anzuelo/línea (ej. Interact en InputSystem_Actions o clic derecho)")]
    [SerializeField] private InputActionReference reelAction;

    [Tooltip("Acción de posición del puntero/mouse (ej. UI/Point en InputSystem_Actions)")]
    [SerializeField] private InputActionReference pointPositionAction;

    [Tooltip("Si es true, el lance se dirige hacia la posición del cursor en pantalla")]
    [SerializeField] private bool aimTowardsPointer = true;

    [Header("Parámetros de Lance (Casting)")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float castDuration = 0.9f;
    [SerializeField] private float castArcHeight = 2.5f;
    [SerializeField] private AnimationCurve castHeightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool orientHookToVelocity = true;

    [Header("Flotación en el Agua (Bobbing)")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2.8f;
    [SerializeField] private float bobAmplitude = 0.1f;

    [Header("Física y Movimiento de la Línea")]
    [Range(10, 60)]
    [SerializeField] private int segmentCount = 30;
    [Tooltip("Cantidad de comba por gravedad cuando la línea está destensada")]
    [SerializeField] private float sagAmount = 0.7f;
    [Tooltip("Rapidez con la que se elimina la comba al alejarse hacia la distancia máxima")]
    [SerializeField] private float tensionTightness = 1.4f;
    [Tooltip("Tiempo de suavizado inercial (retraso del hilo al moverse el anzuelo/caña)")]
    [SerializeField] private float inertiaLag = 0.12f;
    [Tooltip("Intensidad del bucle/látigo aéreo de la línea durante el lance")]
    [SerializeField] private float castWhipIntensity = 1.5f;
    [Tooltip("Frecuencia de la sutil ondulación ambiental/viento")]
    [SerializeField] private float waveFrequency = 2.5f;
    [Tooltip("Amplitud de la ondulación ambiental/viento")]
    [SerializeField] private float waveAmplitude = 0.04f;
    [SerializeField] private float lineWidth = 0.035f;
    [SerializeField] private Color lineColor = new Color(0.85f, 0.92f, 1f, 0.85f);

    [Header("Recogida (Reeling)")]
    [SerializeField] private float defaultReelSpeed = 6f;
    [Tooltip("Mantiene la línea completamente tensa (recta) durante la recogida sin que quede rezagada")]
    [SerializeField] private bool tautLineOnReel = true;

    // Estado actual
    public FishingState CurrentState { get; private set; } = FishingState.IdleAtRod;
    public bool IsCast => CurrentState == FishingState.Casting || CurrentState == FishingState.FloatingInWater;
    public bool IsFloating => CurrentState == FishingState.FloatingInWater;

    // Eventos
    public event Action OnCastStarted;
    public event Action<Vector3> OnHookLanded;
    public event Action OnReelStarted;
    public event Action OnReelCompleted;

    // Simulación de segmentos
    private Vector3[] segmentPositions;
    private Vector3[] segmentVelocities;
    private Coroutine activeCastCoroutine;
    private Coroutine activeReelCoroutine;

    // Control de flotación
    private Vector3 landedWaterPosition;
    private float floatTimer;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        SetupLineRenderer();
        InitializeSegments();
    }

    private void OnEnable()
    {
        EnableAction(castAction);
        EnableAction(reelAction);
        EnableAction(pointPositionAction);
    }

    private void OnDisable()
    {
        DisableAction(castAction);
        DisableAction(reelAction);
        DisableAction(pointPositionAction);
    }

    private void EnableAction(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
            actionRef.action.Enable();
    }

    private void DisableAction(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
            actionRef.action.Disable();
    }

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
        maxDistance = 10f;
        castDuration = 0.9f;
        castArcHeight = 2.5f;
        segmentCount = 30;
        sagAmount = 0.7f;
        tensionTightness = 1.4f;
        inertiaLag = 0.12f;
        castWhipIntensity = 1.5f;
        waveFrequency = 2.5f;
        waveAmplitude = 0.04f;
        lineWidth = 0.035f;
        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.85f;
        lineRenderer.positionCount = segmentCount;

        // Asignar shader para evitar color magenta si no tiene material
        if (lineRenderer.sharedMaterial == null)
        {
            Shader defaultShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (defaultShader == null) defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                Material mat = new Material(defaultShader);
                mat.color = lineColor;
                lineRenderer.material = mat;
            }
        }

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    private void InitializeSegments()
    {
        segmentPositions = new Vector3[segmentCount];
        segmentVelocities = new Vector3[segmentCount];

        Vector3 startPos = pivot != null ? pivot.position : transform.position;
        Vector3 endPos = targetHook != null ? targetHook.position : startPos;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            segmentPositions[i] = Vector3.Lerp(startPos, endPos, t);
            segmentVelocities[i] = Vector3.zero;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = segmentCount;
            lineRenderer.SetPositions(segmentPositions);
        }
    }

    private void Start()
    {
        // Si el anzuelo está inicialmente en descanso, posicionarlo
        if (CurrentState == FishingState.IdleAtRod && hookRestPoint != null && targetHook != null)
        {
            targetHook.position = hookRestPoint.position;
            targetHook.rotation = hookRestPoint.rotation;
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateWaterBobbing();
    }

    private void LateUpdate()
    {
        UpdateLinePhysics();
    }

    #region Manejo de Entrada (Input System)

    private void HandleInput()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        bool castTriggered = false;
        bool reelTriggered = false;

        // Comprobación de acción de lance
        if (castAction != null && castAction.action != null)
        {
            castTriggered = castAction.action.WasPerformedThisFrame();
        }
        else
        {
            // Respaldo directo con New Input System si la referencia aún no está asignada en el inspector
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                castTriggered = true;
            else if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                castTriggered = true;
        }

        // Comprobación de acción de recogida
        if (reelAction != null && reelAction.action != null)
        {
            reelTriggered = reelAction.action.WasPerformedThisFrame();
        }
        else
        {
            // Respaldo directo con New Input System si la referencia aún no está asignada en el inspector
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                reelTriggered = true;
        }

        // Ejecutar lance
        if (castTriggered)
        {
            if (CurrentState == FishingState.IdleAtRod)
            {
                Vector3 targetPos = GetTargetAimPosition();
                Cast(targetPos);
            }
        }

        // Ejecutar recogida
        if (reelTriggered)
        {
            if (CurrentState == FishingState.FloatingInWater || CurrentState == FishingState.Casting)
            {
                Reel(defaultReelSpeed);
            }
        }
    }

    private Vector3 GetTargetAimPosition()
    {
        Vector3 origin = pivot != null ? pivot.position : transform.position;

        if (aimTowardsPointer && mainCamera != null)
        {
            Vector2 screenPos = Vector2.zero;
            bool hasPointerPos = false;

            if (pointPositionAction != null && pointPositionAction.action != null)
            {
                screenPos = pointPositionAction.action.ReadValue<Vector2>();
                hasPointerPos = true;
            }
            else if (Pointer.current != null)
            {
                screenPos = Pointer.current.position.ReadValue();
                hasPointerPos = true;
            }

            if (hasPointerPos)
            {
                float zDistance = Mathf.Abs(mainCamera.transform.position.z - origin.z);
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDistance));
                worldPos.z = origin.z;
                return worldPos;
            }
        }

        // Dirección predeterminada si no hay puntero activo
        Vector3 dir = pivot != null ? (pivot.right + Vector3.down * 0.35f).normalized : Vector3.right;
        return origin + dir * (maxDistance * 0.65f);
    }

    #endregion

    #region Sistema de Lanzamiento (Casting) y Recogida (Reeling)

    /// <summary>
    /// Lanza el anzuelo hacia una posición en el mundo, respetando la distancia máxima.
    /// </summary>
    public void Cast(Vector3 targetWorldPosition)
    {
        if (targetHook == null || pivot == null) return;

        if (activeCastCoroutine != null) StopCoroutine(activeCastCoroutine);
        if (activeReelCoroutine != null) StopCoroutine(activeReelCoroutine);

        // Clampar la posición objetivo dentro del rango maxDistance
        Vector3 origin = pivot.position;
        targetWorldPosition.z = origin.z;
        Vector3 toTarget = targetWorldPosition - origin;
        if (toTarget.magnitude > maxDistance)
        {
            targetWorldPosition = origin + toTarget.normalized * maxDistance;
        }

        activeCastCoroutine = StartCoroutine(CastRoutine(targetWorldPosition));
    }

    /// <summary>
    /// Sobrecarga para lanzar con posición Vector2.
    /// </summary>
    public void Cast(Vector2 targetWorldPosition)
    {
        float z = pivot != null ? pivot.position.z : 0f;
        Cast(new Vector3(targetWorldPosition.x, targetWorldPosition.y, z));
    }

    /// <summary>
    /// Lanza automáticamente a una distancia predeterminada.
    /// </summary>
    public void Cast()
    {
        Vector3 origin = pivot != null ? pivot.position : transform.position;
        Vector3 dir = pivot != null ? (pivot.right + Vector3.down * 0.25f).normalized : Vector3.right;
        Cast(origin + dir * (maxDistance * 0.7f));
    }

    private IEnumerator CastRoutine(Vector3 destination)
    {
        CurrentState = FishingState.Casting;
        OnCastStarted?.Invoke();

        Vector3 startPos = targetHook.position;
        float elapsed = 0f;

        while (elapsed < castDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / castDuration);

            // Trayectoria parabólica: base lineal + elevación por arco
            Vector3 linearPos = Vector3.Lerp(startPos, destination, t);
            float arcFactor = Mathf.Sin(t * Mathf.PI);
            float currentArcHeight = castArcHeight * arcFactor;

            Vector3 currentPos = linearPos + Vector3.up * currentArcHeight;

            // Orientar el anzuelo hacia la velocidad tangencial
            if (orientHookToVelocity)
            {
                Vector3 movementDelta = currentPos - targetHook.position;
                if (movementDelta.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(movementDelta.y, movementDelta.x) * Mathf.Rad2Deg;
                    targetHook.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }
            }

            targetHook.position = currentPos;
            yield return null;
        }

        targetHook.position = destination;
        landedWaterPosition = destination;
        floatTimer = 0f;
        CurrentState = FishingState.FloatingInWater;
        activeCastCoroutine = null;

        OnHookLanded?.Invoke(destination);
    }

    /// <summary>
    /// Recoge la línea de pesca y devuelve el anzuelo a la caña.
    /// </summary>
    public void Reel(float speed = -1f)
    {
        if (speed <= 0f) speed = defaultReelSpeed;
        if (targetHook == null || pivot == null) return;

        if (activeCastCoroutine != null) StopCoroutine(activeCastCoroutine);
        if (activeReelCoroutine != null) StopCoroutine(activeReelCoroutine);

        activeReelCoroutine = StartCoroutine(ReelRoutine(speed));
    }

    private IEnumerator ReelRoutine(float speed)
    {
        CurrentState = FishingState.Reeling;
        OnReelStarted?.Invoke();

        Vector3 destination = hookRestPoint != null ? hookRestPoint.position : pivot.position;

        while (Vector3.Distance(targetHook.position, destination) > 0.05f)
        {
            destination = hookRestPoint != null ? hookRestPoint.position : pivot.position;
            targetHook.position = Vector3.MoveTowards(targetHook.position, destination, speed * Time.deltaTime);

            if (orientHookToVelocity)
            {
                Vector3 dir = (destination - targetHook.position).normalized;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    targetHook.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }
            }

            yield return null;
        }

        targetHook.position = destination;
        if (hookRestPoint != null)
            targetHook.rotation = hookRestPoint.rotation;
        else if (pivot != null)
            targetHook.rotation = pivot.rotation;

        CurrentState = FishingState.IdleAtRod;
        activeReelCoroutine = null;
        OnReelCompleted?.Invoke();
    }

    /// <summary>
    /// Reubica inmediatamente el anzuelo en el punto de descanso.
    /// </summary>
    public void ResetToPivot()
    {
        if (activeCastCoroutine != null) StopCoroutine(activeCastCoroutine);
        if (activeReelCoroutine != null) StopCoroutine(activeReelCoroutine);

        CurrentState = FishingState.IdleAtRod;
        Vector3 restPos = hookRestPoint != null ? hookRestPoint.position : (pivot != null ? pivot.position : transform.position);

        if (targetHook != null)
        {
            targetHook.position = restPos;
            if (hookRestPoint != null) targetHook.rotation = hookRestPoint.rotation;
        }

        InitializeSegments();
    }

    #endregion

    #region Flotación en el Agua (Bobbing)

    private void UpdateWaterBobbing()
    {
        if (CurrentState != FishingState.FloatingInWater || !enableBobbing || targetHook == null) return;

        floatTimer += Time.deltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(floatTimer) * bobAmplitude;
        float gentleTilt = Mathf.Cos(floatTimer * 0.7f) * 6f;

        targetHook.position = new Vector3(
            landedWaterPosition.x,
            landedWaterPosition.y + bobOffset,
            landedWaterPosition.z
        );

        targetHook.rotation = Quaternion.Euler(0f, 0f, gentleTilt);
    }

    #endregion

    #region Simulación Física de la Línea

    private void UpdateLinePhysics()
    {
        if (lineRenderer == null || pivot == null || targetHook == null) return;

        // Si se modificó la cantidad de segmentos en el inspector
        if (segmentPositions == null || segmentPositions.Length != segmentCount)
        {
            InitializeSegments();
        }

        Vector3 startPoint = pivot.position;
        Vector3 endPoint = targetHook.position;

        // Puntos extremos fijados rígidamente
        segmentPositions[0] = startPoint;
        segmentPositions[segmentCount - 1] = endPoint;

        // En recogida (Reeling), la línea está bajo tensión tirando del anzuelo hacia la caña.
        // Se mantiene 100% tensada y recta entre el pivot y el hook, evitando que quede rezagada por detrás.
        if ((CurrentState == FishingState.Reeling && tautLineOnReel) || CurrentState == FishingState.IdleAtRod)
        {
            for (int i = 1; i < segmentCount - 1; i++)
            {
                float u = (float)i / (segmentCount - 1);
                segmentPositions[i] = Vector3.Lerp(startPoint, endPoint, u);
                segmentVelocities[i] = Vector3.zero;
            }
            lineRenderer.SetPositions(segmentPositions);
            return;
        }

        float distance = Vector3.Distance(startPoint, endPoint);
        // Factor de tensión: 0 = muy floja, 1 = completamente tensa
        float tensionFactor = Mathf.Clamp01(distance / (maxDistance * 0.95f));

        // Comba efectiva según tensión, acotada por la distancia real para evitar combas desproporcionadas
        float effectiveSag = Mathf.Min(sagAmount * Mathf.Pow(1f - tensionFactor, tensionTightness), distance * 0.5f);

        float time = Time.time;

        for (int i = 1; i < segmentCount - 1; i++)
        {
            float u = (float)i / (segmentCount - 1);
            // Posición base recta entre caña y anzuelo
            Vector3 basePoint = Vector3.Lerp(startPoint, endPoint, u);

            // Factor de comba parabólica (catenaria aproximada): 0 en los extremos, 1 en el centro
            float sagCurve = 4f * u * (1f - u);

            // 1. Desplazamiento por gravedad (hacia abajo)
            Vector3 gravityOffset = Vector3.down * (effectiveSag * sagCurve);

            // 2. Efecto látigo durante el lance (onda aerodinámica ascendente que se despliega)
            Vector3 whipOffset = Vector3.zero;
            if (CurrentState == FishingState.Casting)
            {
                // Onda que se arquea por encima de la línea durante el vuelo
                float whipShape = Mathf.Sin(u * Mathf.PI);
                whipOffset = Vector3.up * (castWhipIntensity * whipShape);
            }

            // 3. Sutil oscilación ambiental (viento o corriente marina) para mantenerla viva
            float wavePhase = time * waveFrequency + u * Mathf.PI * 2f;
            Vector3 waveOffset = new Vector3(
                Mathf.Sin(wavePhase) * (waveAmplitude * 0.35f),
                Mathf.Cos(wavePhase) * waveAmplitude,
                0f
            ) * sagCurve;

            // Posición objetivo para este segmento
            Vector3 targetSegmentPos = basePoint + gravityOffset + whipOffset + waveOffset;

            // Simulación inercial: amortigua el movimiento hacia la posición objetivo,
            // logrando que la línea se doble y siga con fluidez las curvas de movimiento
            Vector3 currentPos = segmentPositions[i];
            Vector3 currentVel = segmentVelocities[i];

            segmentPositions[i] = Vector3.SmoothDamp(
                currentPos,
                targetSegmentPos,
                ref currentVel,
                inertiaLag
            );

            segmentVelocities[i] = currentVel;
        }

        lineRenderer.SetPositions(segmentPositions);
    }

    #endregion

    #region Visualización en el Editor (Gizmos)

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = pivot != null ? pivot.position : transform.position;

        // Rango máximo de lance
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(origin, maxDistance);

        if (pivot != null && targetHook != null)
        {
            // Línea de referencia
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawLine(pivot.position, targetHook.position);

            // Previsualización del arco de lance en reposo
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.6f);
            Vector3 prevArcPoint = pivot.position;
            int previewSteps = 20;
            for (int i = 1; i <= previewSteps; i++)
            {
                float t = (float)i / previewSteps;
                Vector3 linear = Vector3.Lerp(pivot.position, targetHook.position, t);
                float arc = Mathf.Sin(t * Mathf.PI) * castArcHeight;
                Vector3 currentArcPoint = linear + Vector3.up * arc;
                Gizmos.DrawLine(prevArcPoint, currentArcPoint);
                prevArcPoint = currentArcPoint;
            }
        }
    }

    #endregion
}