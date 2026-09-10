using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TargetPoint : MonoBehaviour
{
    public float angle;
    public float tolerance = 8f; // margen de error en grados
    bool resolved = false;

    Needle needle;
    void Awake() => needle = FindAnyObjectByType<Needle>();
    
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private TMP_Text notifyText;

    private void OnEnable()
    {
        EnableAction(attackAction);
    }

    private void OnDisable()
    {
        DisableAction(attackAction);
    }

    void Start()
    {
        notifyText = GameObject.Find("Notify").GetComponent<TMP_Text>();
    }
    
    void Update()
    {
        if (resolved) return;

        if (attackAction != null && attackAction.action != null && attackAction.action.WasPerformedThisFrame())
        {
            TryHit();
        }
    }

    public void TryHit()
    {
        if (resolved) return;
        float diff = Mathf.Abs(Mathf.DeltaAngle(needle.CurrentAngle, angle));
        if (diff <= tolerance)
        {
            resolved = true;
            OnHit();
        }
        else
        {
            OnMiss();
        }
    }

    void OnHit()  
    {
        Debug.Log("OK"); 
        notifyText.text = "<color=green>¡Bien ahí!</color>";
        GameManager.Instance.SetDamage();
        Destroy(gameObject); 
    }

    void OnMiss() 
    { 
        Debug.Log("Falló");
        notifyText.text = "<color=red>¡Fallaste!</color>"; 
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
}