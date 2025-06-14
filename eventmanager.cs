using System;
using UnityEngine;

/// <summary>
/// Sistema central de eventos para comunicação entre sistemas
/// </summary>
public class EventManager : Singleton<EventManager>
{
    // Eventos do Player
    public static event Action<float, float> OnPlayerHealthChanged; // vida atual, vida máxima
    public static event Action<float, float> OnPlayerManaChanged; // mana atual, mana máxima
    public static event Action OnPlayerDeath;
    public static event Action<int> OnPlayerLevelUp; // novo nível
    public static event Action<int> OnPlayerExperienceGained; // XP ganho
    
    // Eventos de Combate
    public static event Action<float, Vector3> OnDamageDealt; // dano, posição
    public static event Action<GameObject> OnEnemyDeath; // inimigo morto
    public static event Action<string> OnSkillUsed; // nome da skill
    
    // Eventos de Itens
    public static event Action<Item> OnItemPickup; // item coletado
    public static event Action<Item> OnItemDropped; // item dropado
    public static event Action<int> OnGoldChanged; // quantidade de ouro
    
    // Eventos de UI
    public static event Action<bool> OnInventoryToggle; // abrir/fechar inventário
    public static event Action<string> OnShowTooltip; // texto do tooltip
    public static event Action OnHideTooltip;
    
    // Eventos de Quest
    public static event Action<Quest> OnQuestStarted;
    public static event Action<Quest> OnQuestCompleted;
    public static event Action<Quest> OnQuestUpdated;
    
    // Eventos de Game State
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;
    public static event Action OnSceneLoaded;
    
    // Eventos de Input
    public static event Action OnInteractPressed;
    public static event Action OnInventoryToggled;
    public static event Action OnPauseToggled;
    public static event Action<int> OnSkillInputUsed; // skill slot
    
    // Eventos específicos de quest
    public static event Action<string> OnEnemyKilled; // enemy ID
    public static event Action<string> OnItemCollected; // item ID
    public static event Action<string, string> OnItemDelivered; // item ID, NPC ID
    public static event Action<string> OnObjectInteracted; // object ID
    
    // Eventos de UI específicos
    public static event Action<string> OnUIOpened; // UI name
    public static event Action<string> OnUIClosed; // UI name

    // Métodos para disparar eventos - Player
    public static void TriggerPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public static void TriggerPlayerManaChanged(float currentMana, float maxMana)
    {
        OnPlayerManaChanged?.Invoke(currentMana, maxMana);
    }
    
    public static void TriggerPlayerDeath()
    {
        OnPlayerDeath?.Invoke();
    }
    
    public static void TriggerPlayerLevelUp(int newLevel)
    {
        OnPlayerLevelUp?.Invoke(newLevel);
    }
    
    public static void TriggerPlayerExperienceGained(int xpGained)
    {
        OnPlayerExperienceGained?.Invoke(xpGained);
    }
    
    // Métodos para disparar eventos - Combate
    public static void TriggerDamageDealt(float damage, Vector3 position)
    {
        OnDamageDealt?.Invoke(damage, position);
    }
    
    public static void TriggerEnemyDeath(GameObject enemy)
    {
        OnEnemyDeath?.Invoke(enemy);
    }
    
    public static void TriggerSkillUsed(string skillName)
    {
        OnSkillUsed?.Invoke(skillName);
    }
    
    // Métodos para disparar eventos - Itens
    public static void TriggerItemPickup(Item item)
    {
        OnItemPickup?.Invoke(item);
    }
    
    public static void TriggerItemDropped(Item item)
    {
        OnItemDropped?.Invoke(item);
    }
    
    public static void TriggerGoldChanged(int goldAmount)
    {
        OnGoldChanged?.Invoke(goldAmount);
    }
    
    // Métodos para disparar eventos - UI
    public static void TriggerInventoryToggle(bool isOpen)
    {
        OnInventoryToggle?.Invoke(isOpen);
    }
    
    public static void TriggerShowTooltip(string text)
    {
        OnShowTooltip?.Invoke(text);
    }
    
    public static void TriggerHideTooltip()
    {
        OnHideTooltip?.Invoke();
    }
    
    // Métodos para disparar eventos - Quest
    public static void TriggerQuestStarted(Quest quest)
    {
        OnQuestStarted?.Invoke(quest);
    }
    
    public static void TriggerQuestCompleted(Quest quest)
    {
        OnQuestCompleted?.Invoke(quest);
    }
    
    public static void TriggerQuestUpdated(Quest quest)
    {
        OnQuestUpdated?.Invoke(quest);
    }
    
    // Métodos para disparar eventos - Game State
    public static void TriggerGamePaused()
    {
        OnGamePaused?.Invoke();
    }
    
    public static void TriggerGameResumed()
    {
        OnGameResumed?.Invoke();
    }
    
    public static void TriggerGameOver()
    {
        OnGameOver?.Invoke();
    }
    
    public static void TriggerSceneLoaded()
    {
        OnSceneLoaded?.Invoke();
    }
    
    // Métodos para disparar eventos - Input
    public static void TriggerInteractPressed()
    {
        OnInteractPressed?.Invoke();
    }
    
    public static void TriggerInventoryToggled()
    {
        OnInventoryToggled?.Invoke();
    }
    
    public static void TriggerPauseToggled()
    {
        OnPauseToggled?.Invoke();
    }
    
    public static void TriggerSkillUsed(int skillSlot)
    {
        OnSkillInputUsed?.Invoke(skillSlot);
    }
    
    // Métodos para disparar eventos - Quest específicos
    public static void TriggerEnemyKilled(string enemyId)
    {
        OnEnemyKilled?.Invoke(enemyId);
    }
    
    public static void TriggerItemCollected(string itemId)
    {
        OnItemCollected?.Invoke(itemId);
    }
    
    public static void TriggerItemDelivered(string itemId, string npcId)
    {
        OnItemDelivered?.Invoke(itemId, npcId);
    }
    
    public static void TriggerObjectInteracted(string objectId)
    {
        OnObjectInteracted?.Invoke(objectId);
    }
    
    // Métodos para disparar eventos - UI específicos
    public static void TriggerUIOpened(string uiName)
    {
        OnUIOpened?.Invoke(uiName);
    }
    
    public static void TriggerUIClosed(string uiName)
    {
        OnUIClosed?.Invoke(uiName);
    }
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Limpar todos os eventos para evitar vazamentos de memória
        OnPlayerHealthChanged = null;
        OnPlayerManaChanged = null;
        OnPlayerDeath = null;
        OnPlayerLevelUp = null;
        OnPlayerExperienceGained = null;
        OnDamageDealt = null;
        OnEnemyDeath = null;
        OnSkillUsed = null;
        OnItemPickup = null;
        OnItemDropped = null;
        OnGoldChanged = null;
        OnInventoryToggle = null;
        OnShowTooltip = null;
        OnHideTooltip = null;
        OnQuestStarted = null;
        OnQuestCompleted = null;
        OnQuestUpdated = null;
        OnGamePaused = null;
        OnGameResumed = null;
        OnGameOver = null;
        OnSceneLoaded = null;
        OnInteractPressed = null;
        OnInventoryToggled = null;
        OnPauseToggled = null;
        OnSkillInputUsed = null;
        OnEnemyKilled = null;
        OnItemCollected = null;
        OnItemDelivered = null;
        OnObjectInteracted = null;
        OnUIOpened = null;
        OnUIClosed = null;
    }
}