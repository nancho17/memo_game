using UnityEngine;

public class FishController : MonoBehaviour
{
    [SerializeField] private int resistance;
    [SerializeField] private string _name;

    public int currentResistance { get; private set; }
    public string FishName { get; private set; }

    void Awake()
    {
        currentResistance = resistance;
        FishName = _name;
    }

    public void TakeDamage()
    {
        currentResistance--;

        if(currentResistance <= 0)
        {
            GameManager.Instance.EndFishing();
        }
    }
}
