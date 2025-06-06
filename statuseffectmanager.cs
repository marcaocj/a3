using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class StatusEffectManager : MonoBehaviour
{
    [Header("Status Effect Settings")]
    public int maxActiveEffects = 20;
    public bool allowStackingEffects = true;
    public float effectTickRate = 1f;
    
    [Header("Visual Feedback")]
    public Transform effectIconParent;
    public GameObject effectIconPrefab;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Active effects tracking
    private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();
    private Dictionary<StatusEffectType, List<ActiveStatusEffect>> effectsByType = new Dictionary<StatusEffectType, List<ActiveStatusEffect>>();
    private List<StatModifier> activeModifiers = new List<StatModifier>();
    
    // References
    private PlayerStats playerStats;
    private PlayerController playerController;
    
    // Cache and optimization
    private bool modifiersCacheInvalid = true;
    private Dictionary<StatType, float> modifierCache = new Dictionary<StatType, float>();
    
    // Events
    public event Action<StatusEffectType> OnEffectAdded;
    public event Action<StatusEffectType> OnEffectRemoved;
    public event Action<StatusEffectType, int> OnEffectStacked;
    
    public enum StatusEffectType
    {
        Poison,
        Regeneration,
        Strength,
        Weakness,
        Speed,
        Slow,
        Shield,
        Burn,
        Freeze,
        Stun,
        Silence,
        Invisibility,
        Invulnerability,
        CriticalBoost,
        ArmorBoost,
        DamageReduction,
        HealthBoost,
        ManaBoost,
        ExperienceBoost,
        Heal
    }
    
    public enum StackBehavior
    {
        None,           // Não empilha
        Stack,          // Empilha efeitos
        RefreshDuration, // Renova duração
        Replace         // Substitui efeito anterior
    }
    
    [System.Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType effectType;
        public float duration;
        public float remainingTime;
        public float tickInterval;
        public float lastTickTime;
        public int stacks;
        public bool isPersistent;
        public StackBehavior stackBehavior;
        public SkillEffectData effectData;
        public GameObject visualIcon;
        
        public ActiveStatusEffect(StatusEffectType type, float dur, SkillEffectData data)
        {
            effectType = type;
            duration = dur;
            remainingTime = dur;
            effectData = data;
            stacks = 1;
            lastTickTime = 0f;
            tickInterval = data != null ? data.tickInterval : 1f;
            isPersistent = data != null ? data.isPersistent : false;
            stackBehavior = data != null ? data.stackBehavior : StackBehavior.None;
        }
    }
    
    [System.Serializable]
    public class SkillEffectData
    {
        public StatusEffectType effectType;
        public float value;
        public float duration;
        public float tickInterval = 1f;
        public bool isPersistent = false;
        public bool applyOnStart = true;
        public StackBehavior stackBehavior = StackBehavior.None;
        public List<StatModifierData> statModifiers = new List<StatModifierData>();
    }
    
    [System.Serializable]
    public class StatModifierData
    {
        public StatType statType;
        public float value;
        public bool isPercentage;
        
        public StatModifierData(StatType type, float val, bool percentage = false)
        {
            statType = type;
            value = val;
            isPercentage = percentage;
        }
    }
    
    [System.Serializable]
    public class StatModifier
    {
        public StatType statType;
        public float value;
        public bool isPercentage;
        public StatusEffectType sourceEffect;
        
        public StatModifier(StatType type, float val, bool percentage, StatusEffectType source)
        {
            statType = type;
            value = val;
            isPercentage = percentage;
            sourceEffect = source;
        }
    }
    
    public enum StatType
    {
        Health,
        Mana,
        Strength,
        Defense,
        Speed,
        CriticalChance,
        CriticalDamage,
        Experience,
        Armor,
        MagicResistance,
        AttackSpeed,
        MovementSpeed,
        HealthRegeneration,
        ManaRegeneration
    }
    
    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerController = GetComponent<PlayerController>();
        
        InitializeEffectsByType();
    }
    
    private void Start()
    {
        StartCoroutine(EffectTickCoroutine());
    }
    
    private void InitializeEffectsByType()
    {
        foreach (StatusEffectType effectType in System.Enum.GetValues(typeof(StatusEffectType)))
        {
            effectsByType[effectType] = new List<ActiveStatusEffect>();
        }
    }
    
    private IEnumerator EffectTickCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(effectTickRate);
            ProcessActiveEffects();
        }
    }
    
    private void ProcessActiveEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect effect = activeEffects[i];
            
            // Update remaining time
            effect.remainingTime -= effectTickRate;
            
            // Process tick effects
            if (Time.time - effect.lastTickTime >= effect.tickInterval)
            {
                ExecuteTick(effect);
                effect.lastTickTime = Time.time;
            }
            
            // Remove expired effects
            if (effect.remainingTime <= 0 && !effect.isPersistent)
            {
                RemoveEffect(effect);
            }
        }
    }
    
    private void ExecuteTick(ActiveStatusEffect effect)
    {
        if (effect.effectData == null) return;
        
        switch (effect.effectType)
        {
            case StatusEffectType.Poison:
                ApplyDamageOverTime(effect);
                break;
            case StatusEffectType.Regeneration:
                ApplyHealingOverTime(effect);
                break;
            case StatusEffectType.Burn:
                ApplyDamageOverTime(effect);
                break;
            case StatusEffectType.Heal:
                ApplyHealingOverTime(effect);
                break;
        }
    }
    
    private void ApplyDamageOverTime(ActiveStatusEffect effect)
    {
        if (playerStats != null)
        {
            int damage = Mathf.RoundToInt(effect.effectData.value * effect.stacks);
            playerStats.TakeDamage(damage);
            
            if (showDebugInfo)
            {
                Debug.Log($"DOT: {effect.effectType} causou {damage} de dano");
            }
        }
    }
    
    private void ApplyHealingOverTime(ActiveStatusEffect effect)
    {
        if (playerStats != null)
        {
            int healing = Mathf.RoundToInt(effect.effectData.value * effect.stacks);
            playerStats.Heal(healing);
            
            if (showDebugInfo)
            {
                Debug.Log($"HOT: {effect.effectType} curou {healing} de vida");
            }
        }
    }
    
    public void AddEffect(StatusEffectType effectType, float duration, SkillEffectData effectData = null)
    {
        if (activeEffects.Count >= maxActiveEffects && !HasEffect(effectType))
        {
            Debug.LogWarning("Limite máximo de efeitos atingido!");
            return;
        }
        
        // Create effect data if not provided
        if (effectData == null)
        {
            effectData = CreateDefaultEffectData(effectType, duration);
        }
        
        ActiveStatusEffect existingEffect = GetEffect(effectType);
        
        if (existingEffect != null)
        {
            HandleExistingEffect(existingEffect, duration, effectData);
        }
        else
        {
            CreateNewEffect(effectType, duration, effectData);
        }
    }
    
    private SkillEffectData CreateDefaultEffectData(StatusEffectType effectType, float duration)
    {
        SkillEffectData data = new SkillEffectData();
        data.effectType = effectType;
        data.duration = duration;
        
        switch (effectType)
        {
            case StatusEffectType.Poison:
                data.value = 5f;
                data.tickInterval = 1f;
                break;
            case StatusEffectType.Regeneration:
                data.value = 3f;
                data.tickInterval = 1f;
                break;
            case StatusEffectType.Strength:
                data.statModifiers.Add(new StatModifierData(StatType.Strength, 10f));
                data.isPersistent = true;
                break;
            case StatusEffectType.Speed:
                data.statModifiers.Add(new StatModifierData(StatType.MovementSpeed, 1.5f, true));
                data.isPersistent = true;
                break;
        }
        
        return data;
    }
    
    private void HandleExistingEffect(ActiveStatusEffect existingEffect, float duration, SkillEffectData effectData)
    {
        switch (existingEffect.stackBehavior)
        {
            case StackBehavior.None:
                // Não faz nada
                break;
            case StackBehavior.Stack:
                if (allowStackingEffects)
                {
                    existingEffect.stacks++;
                    OnEffectStacked?.Invoke(existingEffect.effectType, existingEffect.stacks);
                }
                break;
            case StackBehavior.RefreshDuration:
                existingEffect.remainingTime = duration;
                existingEffect.duration = duration;
                break;
            case StackBehavior.Replace:
                RemoveEffect(existingEffect);
                CreateNewEffect(effectData.effectType, duration, effectData);
                break;
        }
    }
    
    private void CreateNewEffect(StatusEffectType effectType, float duration, SkillEffectData effectData)
    {
        ActiveStatusEffect newEffect = new ActiveStatusEffect(effectType, duration, effectData);
        
        activeEffects.Add(newEffect);
        effectsByType[effectType].Add(newEffect);
        
        // Apply immediate effects
        if (effectData.applyOnStart)
        {
            ApplyEffectImmediate(newEffect);
        }
        
        // Add stat modifiers
        ApplyStatModifiers(newEffect);
        
        // Create visual icon
        CreateEffectIcon(newEffect);
        
        OnEffectAdded?.Invoke(effectType);
        
        if (showDebugInfo)
        {
            Debug.Log($"Efeito adicionado: {effectType} por {duration} segundos");
        }
    }
    
    private void ApplyEffectImmediate(ActiveStatusEffect effect)
    {
        switch (effect.effectType)
        {
            case StatusEffectType.Heal:
                if (playerStats != null)
                {
                    int healing = Mathf.RoundToInt(effect.effectData.value);
                    playerStats.Heal(healing);
                }
                break;
            case StatusEffectType.Stun:
                // CORREÇÃO: Verificar se PlayerController tem os métodos antes de chamar
                if (playerController != null)
                {
                    playerController.SetStunned(true);
                }
                break;
            case StatusEffectType.Silence:
                // CORREÇÃO: Verificar se PlayerController tem os métodos antes de chamar
                if (playerController != null)
                {
                    playerController.SetSilenced(true);
                }
                break;
        }
    }
    
    private void ApplyStatModifiers(ActiveStatusEffect effect)
    {
        if (effect.effectData?.statModifiers == null) return;
        
        foreach (StatModifierData modData in effect.effectData.statModifiers)
        {
            StatModifier modifier = new StatModifier(modData.statType, modData.value, modData.isPercentage, effect.effectType);
            activeModifiers.Add(modifier);
        }
        
        modifiersCacheInvalid = true;
        UpdatePlayerStats();
    }
    
    private void RemoveStatModifiers(ActiveStatusEffect effect)
    {
        activeModifiers.RemoveAll(mod => mod.sourceEffect == effect.effectType);
        modifiersCacheInvalid = true;
        UpdatePlayerStats();
    }
    
    public void RemoveEffect(StatusEffectType effectType)
    {
        ActiveStatusEffect effect = GetEffect(effectType);
        if (effect != null)
        {
            RemoveEffect(effect);
        }
    }
    
    private void RemoveEffect(ActiveStatusEffect effect)
    {
        // Remove visual effects
        RemoveEffectOnEnd(effect);
        
        // Remove stat modifiers
        RemoveStatModifiers(effect);
        
        // Remove visual icon
        if (effect.visualIcon != null)
        {
            Destroy(effect.visualIcon);
        }
        
        // Remove from collections
        activeEffects.Remove(effect);
        effectsByType[effect.effectType].Remove(effect);
        
        OnEffectRemoved?.Invoke(effect.effectType);
        
        if (showDebugInfo)
        {
            Debug.Log($"Efeito removido: {effect.effectType}");
        }
    }
    
    private void RemoveEffectOnEnd(ActiveStatusEffect effect)
    {
        switch (effect.effectType)
        {
            case StatusEffectType.Stun:
                // CORREÇÃO: Verificar se PlayerController tem os métodos antes de chamar
                if (playerController != null)
                {
                    playerController.SetStunned(false);
                }
                break;
            case StatusEffectType.Silence:
                // CORREÇÃO: Verificar se PlayerController tem os métodos antes de chamar
                if (playerController != null)
                {
                    playerController.SetSilenced(false);
                }
                break;
        }
    }
    
    private void CreateEffectIcon(ActiveStatusEffect effect)
    {
        if (effectIconPrefab == null || effectIconParent == null) return;
        
        GameObject icon = Instantiate(effectIconPrefab, effectIconParent);
        effect.visualIcon = icon;
        
        // Configure icon based on effect type
        StatusEffectIcon iconComponent = icon.GetComponent<StatusEffectIcon>();
        if (iconComponent != null)
        {
            iconComponent.Initialize(effect);
        }
    }
    
    private void UpdatePlayerStats()
    {
        if (playerStats == null) return;
        
        // Recalcular todos os modificadores
        playerStats.RecalculateStats();
    }
    
    public bool HasEffect(StatusEffectType effectType)
    {
        return effectsByType[effectType].Count > 0;
    }
    
    public ActiveStatusEffect GetEffect(StatusEffectType effectType)
    {
        var effects = effectsByType[effectType];
        return effects.Count > 0 ? effects[0] : null;
    }
    
    public List<ActiveStatusEffect> GetAllEffects()
    {
        return new List<ActiveStatusEffect>(activeEffects);
    }
    
    public List<ActiveStatusEffect> GetEffectsOfType(StatusEffectType effectType)
    {
        return new List<ActiveStatusEffect>(effectsByType[effectType]);
    }
    
    public int GetEffectStacks(StatusEffectType effectType)
    {
        ActiveStatusEffect effect = GetEffect(effectType);
        return effect?.stacks ?? 0;
    }
    
    public float GetEffectRemainingTime(StatusEffectType effectType)
    {
        ActiveStatusEffect effect = GetEffect(effectType);
        return effect?.remainingTime ?? 0f;
    }
    
    public bool IsSilenced()
    {
        return HasEffect(StatusEffectType.Silence);
    }
    
    public bool IsStunned()
    {
        return HasEffect(StatusEffectType.Stun);
    }
    
    public bool IsInvisible()
    {
        return HasEffect(StatusEffectType.Invisibility);
    }
    
    public bool IsInvulnerable()
    {
        return HasEffect(StatusEffectType.Invulnerability);
    }
    
    public float GetStatModifier(StatType statType)
    {
        if (modifiersCacheInvalid)
        {
            RecalculateModifierCache();
        }
        
        return modifierCache.ContainsKey(statType) ? modifierCache[statType] : 0f;
    }
    
    private void RecalculateModifierCache()
    {
        modifierCache.Clear();
        
        foreach (StatModifier modifier in activeModifiers)
        {
            if (!modifierCache.ContainsKey(modifier.statType))
            {
                modifierCache[modifier.statType] = 0f;
            }
            
            if (modifier.isPercentage)
            {
                modifierCache[modifier.statType] += modifier.value;
            }
            else
            {
                modifierCache[modifier.statType] += modifier.value;
            }
        }
        
        modifiersCacheInvalid = false;
    }
    
    public void ClearAllEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RemoveEffect(activeEffects[i]);
        }
    }
    
    public void ClearEffectsOfType(StatusEffectType effectType)
    {
        var effects = GetEffectsOfType(effectType);
        foreach (var effect in effects)
        {
            RemoveEffect(effect);
        }
    }
    
    public void ExtendEffectDuration(StatusEffectType effectType, float additionalTime)
    {
        ActiveStatusEffect effect = GetEffect(effectType);
        if (effect != null)
        {
            effect.remainingTime += additionalTime;
            effect.duration += additionalTime;
        }
    }
    
    public void ModifyEffectStacks(StatusEffectType effectType, int stackChange)
    {
        ActiveStatusEffect effect = GetEffect(effectType);
        if (effect != null && allowStackingEffects)
        {
            effect.stacks = Mathf.Max(0, effect.stacks + stackChange);
            
            if (effect.stacks <= 0)
            {
                RemoveEffect(effect);
            }
            else
            {
                OnEffectStacked?.Invoke(effectType, effect.stacks);
            }
        }
    }
    
    private void OnValidate()
    {
        maxActiveEffects = Mathf.Max(1, maxActiveEffects);
        effectTickRate = Mathf.Max(0.1f, effectTickRate);
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
        ClearAllEffects();
    }
}