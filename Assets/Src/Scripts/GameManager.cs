using UnityEngine;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private TMP_Text notify;

    [SerializeField] private GameObject fishingPanel;

    [SerializeField] FishController _currentFish;

    private bool _isCatching;

    public bool IsCatching => _isCatching;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        fishingPanel.SetActive(false);
        _isCatching = false;
        //Debug eliminar despues
        StartCoroutine(DebugFishLoop());
    }

    IEnumerator DebugFishLoop()
    {
        Debug.Log("Empezamos en 5...");
        yield return new WaitForSeconds(5);
        Debug.Log("Iniciando Pesca...");
        StartFishing();
    }

    public void StartFishing()
    {
        if(targetSpawner == null)
        {
            targetSpawner = FindAnyObjectByType<TargetSpawner>();
        }

        notify.text = "Inicia la Captura...";

        _isCatching = true;

        fishingPanel.SetActive(true);

        targetSpawner.SpawnNewTarget();
    }

    public void SetDamage()
    {
        if(_currentFish != null) _currentFish.TakeDamage();
    }

    public void EndFishing()
    {
        notify.text = "¡Captura exitosa! Pescado: " + _currentFish.FishName;
        fishingPanel.SetActive(false);
        _currentFish = null;
        _isCatching = false;
    }
}
