using UnityEngine;
using UnityEngine.InputSystem;

public class DebugFishing : MonoBehaviour
{
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private TargetSpawner targetSpawner;

    private void OnEnable()
    {
        EnableAction(jumpAction);
    }

    private void OnDisable()
    {
        DisableAction(jumpAction);
    }

    void Update()
    {
        if (jumpAction != null && jumpAction.action != null && jumpAction.action.WasPerformedThisFrame())
        {
            targetSpawner.SpawnNewTarget();
        }
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
