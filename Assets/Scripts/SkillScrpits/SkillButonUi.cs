using System;
using System.Collections;
using UnityEngine;

public class SkillButonUi : MonoBehaviour
{
    public SkillConfig config;
    public SkillManager manager;
    public GameObject activeImage;
    public GameObject readyImage;
    public GameObject disabledImage;
    public int duration = 5;

    private void Start()
    {
        manager.OnSkillActivated += UpdateStatu;
        manager.OnSkillDeactivated += UpdateStatu;

        readyImage.SetActive(true);
        activeImage.SetActive(false);
        disabledImage.SetActive(false);
    }

    public void OnButtonClick()
    {
        manager.ToggleSkill(config);
            
    }

    public void UpdateStatu(SkillConfig _config)
    {
        if (_config.Equals(config))
        {
            if (manager.IsSkillActive(_config))
            {
                activeImage.SetActive(true);
                readyImage.SetActive(false);
            }
            else
            {
                activeImage.SetActive(false);
                StartCoroutine(ImageCoroutine(duration));
            }
        }
    }

    IEnumerator ImageCoroutine(int duration)
    {
        disabledImage.SetActive(true);  
        yield return new WaitForSeconds(duration);
        disabledImage.SetActive(false);
        readyImage.SetActive(true);
    }

    public void OnDestroy()
    {
        manager.OnSkillActivated -= UpdateStatu;
        manager.OnSkillDeactivated -= UpdateStatu;
    }
}
