using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class QuestManager : Singleton<QuestManager>
{
    [Header("Quest Settings")]
    public List<Quest> availableQuests = new List<Quest>();
    public int maxActiveQuests = 10;
    public bool showQuestNotifications = true;
    
    [Header("UI References")]
    public GameObject questNotificationPrefab;
    public Transform questNotificationParent;
    public GameObject questLogUI;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Quest tracking
    private List<Quest> activeQuests = new List<Quest>();
    private List<Quest> completedQuests = new List<Quest>();
    private List<Quest> failedQuests = new List<Quest>();
    private Dictionary<string, Quest> questDatabase = new Dictionary<string, Quest>();
    
    // Events
    public event Action<Quest> OnQuestAdded;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestFailed;
    public event Action<Quest> OnQuestProgressUpdated;
    
    // Quest objectives types
    public enum ObjectiveType
    {
        Kill,
        Collect,
        Deliver,
        GoTo,
        Interact,
        Survive,
        Escort,
        Custom
    }
    
    protected override void OnSingletonAwake()
    {
        InitializeQuestSystem();
        SetupEventListeners();
    }
    
    private void Start()
    {
        LoadQuestDatabase();
    }
    
    private void Update()
    {
        UpdateActiveQuests();
    }
    
    private void InitializeQuestSystem()
    {
        // Configurar listas
        if (activeQuests == null) activeQuests = new List<Quest>();
        if (completedQuests == null) completedQuests = new List<Quest>();
        if (failedQuests == null) failedQuests = new List<Quest>();
        if (questDatabase == null) questDatabase = new Dictionary<string, Quest>();
        
        if (showDebugLogs)
        {
            Debug.Log("Quest System inicializado");
        }
    }
    
    private void SetupEventListeners()
    {
        // Usar EventManager diretamente (métodos estáticos)
        EventManager.OnEnemyKilled += HandleEnemyKilled;
        EventManager.OnItemCollected += HandleItemCollected;
        EventManager.OnItemDelivered += HandleItemDelivered;
        EventManager.OnObjectInteracted += HandleObjectInteracted;
    }

    private void LoadQuestDatabase()
    {
        questDatabase.Clear();
        
        // Carregar quests disponíveis no banco de dados
        foreach (Quest quest in availableQuests)
        {
            if (quest != null && !string.IsNullOrEmpty(quest.questId))
            {
                questDatabase[quest.questId] = quest;
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"Carregadas {questDatabase.Count} quests no banco de dados");
        }
    }
    
    private void UpdateActiveQuests()
    {
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            Quest quest = activeQuests[i];
            
            if (quest.hasTimeLimit)
            {
                quest.UpdateTimer(Time.deltaTime);
                
                if (quest.IsFailed())
                {
                    HandleQuestFailed(quest);
                }
            }
        }
    }
    
    // Quest Management Methods
    public bool StartQuest(string questId)
    {
        if (!questDatabase.ContainsKey(questId))
        {
            Debug.LogWarning($"Quest não encontrada: {questId}");
            return false;
        }
        
        Quest questTemplate = questDatabase[questId];
        
        // Verificar se já está ativa
        if (IsQuestActive(questId))
        {
            Debug.LogWarning($"Quest já está ativa: {questId}");
            return false;
        }
        
        // Verificar se pode iniciar
        if (!questTemplate.CanStart())
        {
            Debug.LogWarning($"Quest não pode ser iniciada: {questId}");
            return false;
        }
        
        // Verificar limite de quests ativas
        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning("Limite máximo de quests ativas atingido");
            return false;
        }
        
        // Criar cópia da quest para ativação
        Quest activeQuest = questTemplate.Clone();
        activeQuest.StartQuest();
        
        activeQuests.Add(activeQuest);
        
        // Configurar eventos específicos da quest
        activeQuest.OnQuestCompleted += HandleQuestCompleted;
        activeQuest.OnQuestFailed += HandleQuestFailed;
        activeQuest.OnProgressUpdated += HandleQuestProgressUpdated;
        
        OnQuestAdded?.Invoke(activeQuest);
        
        ShowQuestNotification($"Nova Quest: {activeQuest.questName}", NotificationType.QuestStarted);
        
        if (showDebugLogs)
        {
            Debug.Log($"Quest iniciada: {activeQuest.questName}");
        }
        
        return true;
    }
    
    public void CompleteQuest(string questId)
    {
        Quest quest = GetActiveQuest(questId);
        if (quest != null && quest.IsActive())
        {
            quest.CompleteQuest();
        }
    }
    
    public void FailQuest(string questId)
    {
        Quest quest = GetActiveQuest(questId);
        if (quest != null && quest.IsActive())
        {
            quest.FailQuest();
        }
    }
    
    public void UpdateQuestProgress(string questId, int amount = 1)
    {
        Quest quest = GetActiveQuest(questId);
        if (quest != null && quest.IsActive())
        {
            quest.UpdateProgress(amount);
        }
    }
    
    public void UpdateQuestProgress(Quest.QuestType questType, string targetId, int amount = 1)
    {
        var relevantQuests = activeQuests.Where(q => 
            q.questType == questType && 
            (string.IsNullOrEmpty(q.targetObjectId) || q.targetObjectId == targetId)
        ).ToList();
        
        foreach (Quest quest in relevantQuests)
        {
            quest.UpdateProgress(amount);
        }
    }
    
    // Event Handlers
    private void HandleEnemyKilled(string enemyId)
    {
        UpdateQuestProgress(Quest.QuestType.Kill, enemyId);
    }
    
    private void HandleItemCollected(string itemId)
    {
        UpdateQuestProgress(Quest.QuestType.Collect, itemId);
    }
    
    private void HandleItemDelivered(string itemId, string npcId)
    {
        UpdateQuestProgress(Quest.QuestType.Deliver, itemId);
    }
    
    private void HandleObjectInteracted(string objectId)
    {
        UpdateQuestProgress(Quest.QuestType.Interact, objectId);
    }
    
    private void HandleQuestCompleted(Quest quest)
    {
        activeQuests.Remove(quest);
        completedQuests.Add(quest);
        
        // Dar recompensas
        quest.GiveRewards();
        
        OnQuestCompleted?.Invoke(quest);
        
        ShowQuestNotification($"Quest Completa: {quest.questName}", NotificationType.QuestCompleted);
        
        // Iniciar próxima quest se existir
        if (!string.IsNullOrEmpty(quest.nextQuestId))
        {
            StartQuest(quest.nextQuestId);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"Quest completada: {quest.questName}");
        }
    }
    
    private void HandleQuestFailed(Quest quest)
    {
        activeQuests.Remove(quest);
        failedQuests.Add(quest);
        
        OnQuestFailed?.Invoke(quest);
        
        ShowQuestNotification($"Quest Falhada: {quest.questName}", NotificationType.QuestFailed);
        
        if (showDebugLogs)
        {
            Debug.Log($"Quest falhada: {quest.questName}");
        }
    }
    
    private void HandleQuestProgressUpdated(Quest quest, int progressAmount)
    {
        OnQuestProgressUpdated?.Invoke(quest);
        
        if (showQuestNotifications && progressAmount > 0)
        {
            ShowQuestNotification($"{quest.questName}: {quest.GetProgressText()}", NotificationType.QuestProgress);
        }
    }
    
    // Query Methods
    public Quest GetActiveQuest(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.questId == questId);
    }
    
    public Quest GetCompletedQuest(string questId)
    {
        return completedQuests.FirstOrDefault(q => q.questId == questId);
    }
    
    public Quest GetQuestFromDatabase(string questId)
    {
        return questDatabase.ContainsKey(questId) ? questDatabase[questId] : null;
    }
    
    public List<Quest> GetActiveQuests()
    {
        return new List<Quest>(activeQuests);
    }
    
    public List<Quest> GetCompletedQuests()
    {
        return new List<Quest>(completedQuests);
    }
    
    public List<Quest> GetFailedQuests()
    {
        return new List<Quest>(failedQuests);
    }
    
    public List<Quest> GetAvailableQuests()
    {
        return availableQuests.Where(q => q.CanStart() && !IsQuestActive(q.questId) && !IsQuestCompleted(q.questId)).ToList();
    }
    
    public bool IsQuestActive(string questId)
    {
        return activeQuests.Any(q => q.questId == questId);
    }
    
    public bool IsQuestCompleted(string questId)
    {
        return completedQuests.Any(q => q.questId == questId);
    }
    
    public bool IsQuestFailed(string questId)
    {
        return failedQuests.Any(q => q.questId == questId);
    }
    
    public int GetActiveQuestCount()
    {
        return activeQuests.Count;
    }
    
    public int GetCompletedQuestCount()
    {
        return completedQuests.Count;
    }
    
    public QuestStatistics GetQuestStatistics()
    {
        return new QuestStatistics
        {
            completedQuests = completedQuests.Count,
            activeQuests = activeQuests.Count,
            failedQuests = failedQuests.Count,
            totalQuests = questDatabase.Count
        };
    }
    
    // Notification System
    private void ShowQuestNotification(string message, NotificationType type)
    {
        if (!showQuestNotifications || questNotificationPrefab == null) return;
        
        GameObject notification = null;
        
        if (questNotificationParent != null)
        {
            notification = Instantiate(questNotificationPrefab, questNotificationParent);
        }
        else
        {
            notification = Instantiate(questNotificationPrefab);
        }
        
        QuestNotification notificationComponent = notification.GetComponent<QuestNotification>();
        
        if (notificationComponent != null)
        {
            notificationComponent.ShowNotification(message, type);
        }
        else
        {
            Debug.LogWarning("QuestNotification component não encontrado no prefab!");
            // Fallback: apenas log da notificação
            Debug.Log($"[Quest Notification] {type}: {message}");
            if (notification != null) Destroy(notification);
        }
    }
    
    public enum NotificationType
    {
        QuestStarted,
        QuestCompleted,
        QuestFailed,
        QuestProgress
    }
    
    // UI Methods
    public void OpenQuestLog()
    {
        if (questLogUI != null)
        {
            questLogUI.SetActive(true);
            EventManager.TriggerUIOpened("QuestLog");
        }
    }
    
    public void CloseQuestLog()
    {
        if (questLogUI != null)
        {
            questLogUI.SetActive(false);
            EventManager.TriggerUIClosed("QuestLog");
        }
    }
    
    public void ToggleQuestLog()
    {
        if (questLogUI != null)
        {
            bool isActive = questLogUI.activeSelf;
            if (isActive)
            {
                CloseQuestLog();
            }
            else
            {
                OpenQuestLog();
            }
        }
    }
    
    // Save/Load System
    public QuestSaveData GetSaveData()
    {
        QuestSaveData saveData = new QuestSaveData();
        
        saveData.activeQuestIds = activeQuests.Select(q => q.questId).ToList();
        saveData.completedQuestIds = completedQuests.Select(q => q.questId).ToList();
        saveData.failedQuestIds = failedQuests.Select(q => q.questId).ToList();
        
        saveData.questData = new Dictionary<string, Dictionary<string, object>>();
        foreach (Quest quest in activeQuests)
        {
            saveData.questData[quest.questId] = quest.ToSaveData();
        }
        
        return saveData;
    }
    
    public void LoadSaveData(QuestSaveData saveData)
    {
        if (saveData == null) return;
        
        // Limpar estado atual
        activeQuests.Clear();
        completedQuests.Clear();
        failedQuests.Clear();
        
        // Carregar quests completas
        foreach (string questId in saveData.completedQuestIds)
        {
            if (questDatabase.ContainsKey(questId))
            {
                Quest quest = questDatabase[questId].Clone();
                quest.status = Quest.QuestStatus.Completed;
                completedQuests.Add(quest);
            }
        }
        
        // Carregar quests falhadas
        foreach (string questId in saveData.failedQuestIds)
        {
            if (questDatabase.ContainsKey(questId))
            {
                Quest quest = questDatabase[questId].Clone();
                quest.status = Quest.QuestStatus.Failed;
                failedQuests.Add(quest);
            }
        }
        
        // Carregar quests ativas
        foreach (string questId in saveData.activeQuestIds)
        {
            if (questDatabase.ContainsKey(questId))
            {
                Quest quest = questDatabase[questId].Clone();
                quest.status = Quest.QuestStatus.Active;
                
                // Carregar dados específicos se disponíveis
                if (saveData.questData.ContainsKey(questId))
                {
                    quest.LoadFromSaveData(saveData.questData[questId]);
                }
                
                activeQuests.Add(quest);
                
                // Reconfigurar eventos
                quest.OnQuestCompleted += HandleQuestCompleted;
                quest.OnQuestFailed += HandleQuestFailed;
                quest.OnProgressUpdated += HandleQuestProgressUpdated;
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"Carregadas {activeQuests.Count} quests ativas, {completedQuests.Count} completas, {failedQuests.Count} falhadas");
        }
    }
    
    [System.Serializable]
    public class QuestSaveData
    {
        public List<string> activeQuestIds = new List<string>();
        public List<string> completedQuestIds = new List<string>();
        public List<string> failedQuestIds = new List<string>();
        public Dictionary<string, Dictionary<string, object>> questData = new Dictionary<string, Dictionary<string, object>>();
    }
    
    [System.Serializable]
    public class QuestStatistics
    {
        public int completedQuests;
        public int activeQuests;
        public int failedQuests;
        public int totalQuests;
    }
    
    // Cleanup
    protected override void OnDestroy()
    {
        EventManager.OnEnemyKilled -= HandleEnemyKilled;
        EventManager.OnItemCollected -= HandleItemCollected;
        EventManager.OnItemDelivered -= HandleItemDelivered;
        EventManager.OnObjectInteracted -= HandleObjectInteracted;
        
        // Limpar eventos das quests ativas
        foreach (Quest quest in activeQuests)
        {
            quest.OnQuestCompleted -= HandleQuestCompleted;
            quest.OnQuestFailed -= HandleQuestFailed;
            quest.OnProgressUpdated -= HandleQuestProgressUpdated;
        }
        
        base.OnDestroy();
    }
    
    // Debug Methods
    [ContextMenu("Debug - List Active Quests")]
    private void DebugListActiveQuests()
    {
        Debug.Log($"=== QUESTS ATIVAS ({activeQuests.Count}) ===");
        foreach (Quest quest in activeQuests)
        {
            Debug.Log($"- {quest.questName} ({quest.questId}): {quest.GetProgressText()}");
        }
    }
    
    [ContextMenu("Debug - Complete All Active Quests")]
    private void DebugCompleteAllQuests()
    {
        var questsToComplete = new List<Quest>(activeQuests);
        foreach (Quest quest in questsToComplete)
        {
            quest.CompleteQuest();
        }
    }
}

// Componente auxiliar para notificações de quest
public class QuestNotification : MonoBehaviour
{
    [Header("UI References")]
    public TMPro.TextMeshProUGUI messageText;
    public UnityEngine.UI.Image backgroundImage;
    public float displayDuration = 3f;
    public float fadeSpeed = 2f;
    
    [Header("Colors")]
    public Color startedColor = Color.green;
    public Color completedColor = Color.blue;
    public Color failedColor = Color.red;
    public Color progressColor = Color.yellow;
    
    public void ShowNotification(string message, QuestManager.NotificationType type)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        if (backgroundImage != null)
        {
            Color targetColor = startedColor;
            switch (type)
            {
                case QuestManager.NotificationType.QuestStarted: targetColor = startedColor; break;
                case QuestManager.NotificationType.QuestCompleted: targetColor = completedColor; break;
                case QuestManager.NotificationType.QuestFailed: targetColor = failedColor; break;
                case QuestManager.NotificationType.QuestProgress: targetColor = progressColor; break;
            }
            backgroundImage.color = targetColor;
        }
        
        StartCoroutine(DisplayAndFade());
    }
    
    private System.Collections.IEnumerator DisplayAndFade()
    {
        // Aparecer
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        // Aguardar
        yield return new WaitForSeconds(displayDuration);
        
        // Desaparecer
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
