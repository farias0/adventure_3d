using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private VisualElement mHealthBarFill;
    private float mCurrentHealth;

    private const float MaxHealth = 100f;

    /// <summary>
    /// There should only be a single instance per scene
    /// </summary>
    public static HUDController Instance { get; private set; }


    public void PlayerTakeDamage(float amount)
    {
        mCurrentHealth -= amount;
        if (mCurrentHealth < 0) mCurrentHealth = 0;
        UpdateHealthBar();
    }

    public void PlayerHeal(float amount)
    {
        mCurrentHealth += amount;
        if (mCurrentHealth > MaxHealth) mCurrentHealth = MaxHealth;
        UpdateHealthBar();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        mHealthBarFill = root.Q<VisualElement>("HealthBarFill");
        
        mCurrentHealth = MaxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthPercentage = mCurrentHealth / MaxHealth;
        mHealthBarFill.style.width = Length.Percent(healthPercentage * 100f);
    }
}
