using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private TMP_Text notify;

    [SerializeField] private GameObject fishingPanel;

    [SerializeField] private List<FishController> fish;
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
        //TODO: eliminar, el llamado lo debe hacer es el pez al ser enganchado
        StartCoroutine(DebugFishLoop());
    }

    IEnumerator DebugFishLoop()
    {
        _currentFish = fish[Random.Range(0, fish.Count)];
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
        fish.Remove(_currentFish);
        Destroy(_currentFish.gameObject);
        notify.text = "¡Captura exitosa! Pescado: " + _currentFish.FishName;
        fishingPanel.SetActive(false);
        if(fish.Count == 0)
        {
            notify.text = "¡Felicidades! Has pescado todos los peces.";
            return;
        }
        _currentFish = fish[Random.Range(0, fish.Count)];
        _isCatching = false;
        StartCoroutine(DebugFishLoop());
    }
}
