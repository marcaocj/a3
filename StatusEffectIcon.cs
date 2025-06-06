using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente para ícones de status effects na UI
/// </summary>
public class StatusEffectIcon : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public Text durationText;
    public Text stacksText;
    
    [Header("Visual Settings")]
    public Color beneficialColor = Color.green;
    public Color harmfulColor = Color.red;
    public float pulseSpeed = 2f;
    
    private StatusEffectManager.ActiveStatusEffect activeEffect;
    private float originalAlpha;
    
    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();
            
        if (iconImage != null)
            originalAlpha = iconImage.color.a;
    }
    
    private void Update()
    {
        if (activeEffect != null)
        {
            UpdateIcon();
        }
    }
    
    /// <summary>
    /// Inicializa o ícone com um efeito ativo
    /// </summary>
    public void Initialize(StatusEffectManager.ActiveStatusEffect effect)
    {
        activeEffect = effect;
        
        if (iconImage != null && effect.effectData != null)
        {
            // Definir cor baseada se é benéfico ou não
            Color targetColor = effect.effectData.isPersistent ? beneficialColor : harmfulColor;
            iconImage.color = targetColor;
        }
        
        UpdateIcon();
    }
    
    private void UpdateIcon()
    {
        if (activeEffect == null) return;
        
        // Atualizar texto de duração
        if (durationText != null && !activeEffect.isPersistent)
        {
            durationText.text = Mathf.Ceil(activeEffect.remainingTime).ToString();
        }
        
        // Atualizar texto de stacks
        if (stacksText != null && activeEffect.stacks > 1)
        {
            stacksText.text = activeEffect.stacks.ToString();
            stacksText.gameObject.SetActive(true);
        }
        else if (stacksText != null)
        {
            stacksText.gameObject.SetActive(false);
        }
        
        // Efeito de pulsação quando está prestes a expirar
        if (iconImage != null && activeEffect.remainingTime < 5f && !activeEffect.isPersistent)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            Color currentColor = iconImage.color;
            currentColor.a = Mathf.Lerp(0.3f, originalAlpha, pulse);
            iconImage.color = currentColor;
        }
    }
}