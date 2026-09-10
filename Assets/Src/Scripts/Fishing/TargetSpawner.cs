using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Transform container;

    private GameObject currentTarget;

    private void Start()
    {
        SpawnNewTarget();
    }

    public void SpawnNewTarget()
    {
        if (currentTarget != null)
        {
            Destroy(currentTarget);
        }
        currentTarget = SpawnTarget(Random.Range(0f, 360f));
    }

    public void Update()
    {
        if(!GameManager.Instance.IsCatching) return;

        if(currentTarget == null)
        {
            SpawnNewTarget();
        }
    }

    public GameObject SpawnTarget(float angleDegrees)
    {
        GameObject t = Instantiate(targetPrefab, container);
        RectTransform rt = t.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        //gira en mismo sentido que el azuelo en el circulo
        rt.localEulerAngles = new Vector3(0f, 0f, -angleDegrees);

        TargetPoint tp = t.GetComponent<TargetPoint>();
        if (tp != null)
        {
            tp.angle = angleDegrees;
        }

        return t;
    }
}
