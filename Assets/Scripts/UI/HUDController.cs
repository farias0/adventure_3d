using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private VisualElement mHealthBarFill;
    private float mMaxHealth;
    private float mCurrentHealth;


    /// <summary>
    /// There should only be a single instance per scene
    /// </summary>
    public static HUDController Instance { get; private set; }

    public void PlayerSetHealth(int health)
    {
        mCurrentHealth = health;
        if (mCurrentHealth < 0) mCurrentHealth = 0;
        if (mCurrentHealth > mMaxHealth) mCurrentHealth = mMaxHealth;
        UpdateHealthBar();
    }

    public void PlayerSetMaxHealth(int health)
    {
        mMaxHealth = health;
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
        
        mCurrentHealth = mMaxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthPercentage = mCurrentHealth / mMaxHealth;
        mHealthBarFill.style.width = Length.Percent(healthPercentage * 100f);
    }
}
