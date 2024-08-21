using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private VisualElement mRoot;
    private VisualElement mHealthBarFill;
    private VisualElement mStaminaBarFill;
    private float mMaxHealth;
    private float mCurrentHealth;
    private float mMaxStamina;
    private float mCurrentStamina;


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
        UpdateHealthBarMax();
    }

    public void PlayerSetStamina(float stamina)
    {
        mCurrentStamina = stamina;
        if (mCurrentStamina < 0) mCurrentStamina = 0;
        if (mCurrentStamina > mMaxStamina) mCurrentStamina = mMaxStamina;
        UpdateStaminaBar();
    }

    public void PlayerSetMaxStamina(float stamina)
    {
        mMaxStamina = stamina;
        UpdateStaminaBarMax();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (active) SyncWithState();
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
        mRoot = GetComponent<UIDocument>().rootVisualElement;
        mHealthBarFill = mRoot.Q<VisualElement>("HealthBarFill");
        mStaminaBarFill = mRoot.Q<VisualElement>("StaminaBarFill");
        
        mCurrentHealth = mMaxHealth;
        UpdateHealthBar();

        mCurrentStamina = mMaxStamina;
        UpdateStaminaBar();
    }

    private void UpdateHealthBar()
    {
        float healthPercentage = mCurrentHealth / mMaxHealth;
        mHealthBarFill.style.width = Length.Percent(healthPercentage * 100f);
    }

    private void UpdateHealthBarMax()
    {
        var healthBarContainer = mRoot.Q<VisualElement>("HealthBarContainer");
        healthBarContainer.style.width = Length.Percent(mMaxHealth / 4.0f);
    }

    private void UpdateStaminaBar()
    {
        float staminaPercentage = mCurrentStamina / mMaxStamina;
        mStaminaBarFill.style.width = Length.Percent(staminaPercentage * 100f);
    }

    private void UpdateStaminaBarMax()
    {
        var staminaBarContainer = mRoot.Q<VisualElement>("StaminaBarContainer");
        staminaBarContainer.style.width = Length.Percent(mMaxStamina / 4.0f);
    }

    private void SyncWithState()
    {
        UpdateHealthBarMax();
        UpdateStaminaBarMax();
        UpdateHealthBar();
        UpdateStaminaBar();
    }
}
