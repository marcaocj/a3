using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Health Bar Components")]
    public Slider healthSlider;
    public Slider healthSliderBackground;
    public Image healthFill;
    public Image healthBackground;
    
    [Header("Colors")]
    public Color healthColor = Color.green;
    public Color lowHealthColor = new Color(0.5f, 0f, 0f, 1f); // Dark red equivalente
    public Color criticalHealthColor = Color.red;
    public Color manaColor = new Color(0f, 0f, 0.5f, 1f); // Dark blue equivalente
    public Color backgroundColorNormal = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color backgroundColorDamaged = new Color(0.8f, 0.2f, 0.2f, 0.8f);
    
    [Header("Animation Settings")]
    public float smoothSpeed = 2f;
    public float damageFlashDuration = 0.2f;
    public bool animateHealthChanges = true;
    
    [Header("Thresholds")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;
    [Range(0f, 1f)]
    public float criticalHealthThreshold = 0.15f;
    
    [Header("Mana Bar (Optional)")]
    public Slider manaSlider;
    public Image manaFill;
    
    private PlayerStats playerStats;
    private float targetHealthPercentage;
    private float currentDisplayedHealth;
    private float targetManaPercentage;
    private float currentDisplayedMana;
    private bool isFlashing = false;
    
    private Color originalBackgroundColor;
    
    private void Start()
    {
        InitializeHealthBar();
        FindPlayerStats();
    }
    
    private void InitializeHealthBar()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();
            
        if (healthFill == null && healthSlider != null)
            healthFill = healthSlider.fillRect.GetComponent<Image>();
            
        if (healthBackground == null && healthSliderBackground != null)
            healthBackground = healthSliderBackground.fillRect.GetComponent<Image>();
            
        // Configurar valores iniciais
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
        
        if (manaSlider != null)
        {
            manaSlider.minValue = 0f;
            manaSlider.maxValue = 1f;
            manaSlider.value = 1f;
        }
        
        // Salvar cor original do background
        if (healthBackground != null)
            originalBackgroundColor = healthBackground.color;
        
        currentDisplayedHealth = 1f;
        currentDisplayedMana = 1f;
    }
    
    private void FindPlayerStats()
    {
        if (playerStats == null)
        {
            // Usar a nova função recomendada
            playerStats = FindFirstObjectByType<PlayerStats>();
            
            if (playerStats == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }
        }
    }
    
    private void Update()
    {
        if (playerStats == null)
        {
            FindPlayerStats();
            return;
        }
        
        UpdateHealthDisplay();
        UpdateManaDisplay();
    }
    
    private void UpdateHealthDisplay()
    {
        targetHealthPercentage = (float)playerStats.CurrentHealth / playerStats.MaxHealth;
        
        if (animateHealthChanges)
        {
            // Animação suave para mudanças de vida
            currentDisplayedHealth = Mathf.Lerp(currentDisplayedHealth, targetHealthPercentage, Time.deltaTime * smoothSpeed);
        }
        else
        {
            currentDisplayedHealth = targetHealthPercentage;
        }
        
        // Atualizar slider
        if (healthSlider != null)
        {
            healthSlider.value = currentDisplayedHealth;
        }
        
        // Atualizar cor baseada na vida atual
        UpdateHealthColor();
        
        // Verificar se deve mostrar efeito de dano
        if (Mathf.Abs(currentDisplayedHealth - targetHealthPercentage) > 0.01f)
        {
            if (!isFlashing && targetHealthPercentage < currentDisplayedHealth)
            {
                StartCoroutine(FlashDamage());
            }
        }
    }
    
    private void UpdateManaDisplay()
    {
        if (manaSlider == null || playerStats.MaxMana <= 0) return;
        
        targetManaPercentage = (float)playerStats.CurrentMana / playerStats.MaxMana;
        
        if (animateHealthChanges)
        {
            currentDisplayedMana = Mathf.Lerp(currentDisplayedMana, targetManaPercentage, Time.deltaTime * smoothSpeed);
        }
        else
        {
            currentDisplayedMana = targetManaPercentage;
        }
        
        manaSlider.value = currentDisplayedMana;
        
        if (manaFill != null)
        {
            manaFill.color = manaColor;
        }
    }
    
    private void UpdateHealthColor()
    {
        if (healthFill == null) return;
        
        Color targetColor;
        
        if (currentDisplayedHealth <= criticalHealthThreshold)
        {
            targetColor = criticalHealthColor;
        }
        else if (currentDisplayedHealth <= lowHealthThreshold)
        {
            // Interpolação entre cor baixa e crítica
            float t = (currentDisplayedHealth - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold);
            targetColor = Color.Lerp(criticalHealthColor, lowHealthColor, t);
        }
        else
        {
            // Interpolação entre cor normal e baixa
            float t = (currentDisplayedHealth - lowHealthThreshold) / (1f - lowHealthThreshold);
            targetColor = Color.Lerp(lowHealthColor, healthColor, t);
        }
        
        healthFill.color = targetColor;
    }
    
    private IEnumerator FlashDamage()
    {
        isFlashing = true;
        
        if (healthBackground != null)
        {
            // Flash do background
            healthBackground.color = backgroundColorDamaged;
            yield return new WaitForSeconds(damageFlashDuration);
            
            // Voltar cor original gradualmente
            float elapsed = 0f;
            while (elapsed < damageFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageFlashDuration;
                healthBackground.color = Color.Lerp(backgroundColorDamaged, originalBackgroundColor, t);
                yield return null;
            }
            
            healthBackground.color = originalBackgroundColor;
        }
        
        isFlashing = false;
    }
    
    public void SetPlayer(PlayerStats newPlayerStats)
    {
        playerStats = newPlayerStats;
        if (playerStats != null)
        {
            currentDisplayedHealth = (float)playerStats.CurrentHealth / playerStats.MaxHealth;
            currentDisplayedMana = (float)playerStats.CurrentMana / playerStats.MaxMana;
        }
    }
    
    public void ForceUpdateDisplay()
    {
        if (playerStats == null) return;
        
        targetHealthPercentage = (float)playerStats.CurrentHealth / playerStats.MaxHealth;
        currentDisplayedHealth = targetHealthPercentage;
        
        if (healthSlider != null)
        {
            healthSlider.value = currentDisplayedHealth;
        }
        
        UpdateHealthColor();
        
        if (manaSlider != null && playerStats.MaxMana > 0)
        {
            targetManaPercentage = (float)playerStats.CurrentMana / playerStats.MaxMana;
            currentDisplayedMana = targetManaPercentage;
            manaSlider.value = currentDisplayedMana;
        }
    }
    
    public void SetHealthBarVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    public void SetAnimationEnabled(bool enabled)
    {
        animateHealthChanges = enabled;
    }
    
    private void OnValidate()
    {
        // Garantir que os thresholds estejam em ordem correta
        if (criticalHealthThreshold > lowHealthThreshold)
        {
            criticalHealthThreshold = lowHealthThreshold;
        }
    }
}