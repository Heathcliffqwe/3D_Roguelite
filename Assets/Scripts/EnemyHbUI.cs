using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHbUI : MonoBehaviour
{
    public static EnemyHbUI Instance;
    public Image HealthBar;
    public TextMeshProUGUI HealthText;
    public GameObject HealthBarObj;

    private void Start()
    {
        HealthBarObj.SetActive(false);
    }

    private void Awake()
    {
        Instance = this;
    }

    public void ShowEnemy(string name, float health)
    {
        HealthBarObj.SetActive(true);
        HealthText.text = name;
        HealthBar.fillAmount = health;
    }

    public void Hide()
    {
        HealthBarObj.SetActive(false);
    }
    
}
