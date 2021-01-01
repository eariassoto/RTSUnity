using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private Health health = null;
    [SerializeField] private GameObject healthBarParent = null;
    [SerializeField] private Image HealthBarImage = null;

    private void Awake()
    {
        health.ClientOnHeathUpdated += HandleHealthUpdated;
    }

    private void OnDestroy()
    {
        health.ClientOnHeathUpdated -= HandleHealthUpdated;
    }

    private void OnMouseEnter()
    {
        healthBarParent.SetActive(true);
    }

    private void OnMouseExit()
    {
        healthBarParent.SetActive(false);
    }

    private void HandleHealthUpdated(int current, int maxHealth)
    {
        HealthBarImage.fillAmount = (float)current / maxHealth;
    }
}
