using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class Quest
{
    [Header("Quest Information")]
    public string questId;
    public string questName;
    [TextArea(3, 5)]
    public string questDescription;
    public QuestType questType;
    public QuestStatus status;
    
    [Header("Quest Parameters")]
    public bool isMainQuest = false;
    public int targetAmount = 1;
    public int currentProgress = 0;
    public string targetObjectId;
    public Vector3 targetLocation;
    public float targetRadius = 5f;
    
    [Header("Rewards")]
    public int experienceReward = 100;
    public int goldReward = 50;
    public List<ItemReward> itemRewards = new List<ItemReward>();
    
    [Header("Quest Chain")]
    public string nextQuestId;
    public List<string> prerequisiteQuestIds = new List<string>();
    
    [Header("Time Limit")]
    public bool hasTimeLimit = false;
    public float timeLimit = 300f; // 5 minutos por padrão
    private float timeRemaining;
    
    [Header("Optional Settings")]
    public bool isRepeatable = false;
    public bool isHidden = false;
    public int requiredLevel = 1;
    
    // Eventos
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestFailed;
    public event Action<Quest, int> OnProgressUpdated;
    
    public enum QuestType
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
    
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed,
        TurnedIn
    }
    
    [System.Serializable]
    public class ItemReward
    {
        public string itemId;
        public int quantity = 1;
    }
    
    public Quest()
    {
        questId = System.Guid.NewGuid().ToString();
        status = QuestStatus.NotStarted;
        timeRemaining = timeLimit;
    }
    
    public Quest(string id, string name, string description, QuestType type)
    {
        questId = id;
        questName = name;
        questDescription = description;
        questType = type;
        status = QuestStatus.NotStarted;
        timeRemaining = timeLimit;
    }
    
    public void StartQuest()
    {
        if (status != QuestStatus.NotStarted) return;
        
        status = QuestStatus.Active;
        currentProgress = 0;
        timeRemaining = timeLimit;
        
        Debug.Log($"Quest iniciada: {questName}");
        
        // Notificar sistema de quests
        if (QuestManager.Instance != null)
        {
            EventManager.TriggerQuestStarted(this);
        }
        
        // Registrar listeners baseado no tipo de quest
        RegisterQuestListeners();
    }
    
    public void UpdateProgress(int amount = 1)
    {
        if (status != QuestStatus.Active) return;
        
        int previousProgress = currentProgress;
        currentProgress = Mathf.Min(currentProgress + amount, targetAmount);
        
        OnProgressUpdated?.Invoke(this, currentProgress - previousProgress);
        
        Debug.Log($"Quest {questName}: {currentProgress}/{targetAmount}");
        
        if (currentProgress >= targetAmount)
        {
            CompleteQuest();
        }
    }
    
    public void CompleteQuest()
    {
        if (status != QuestStatus.Active) return;
        
        status = QuestStatus.Completed;
        
        Debug.Log($"Quest completada: {questName}");
        
        OnQuestCompleted?.Invoke(this);
        
        // Notificar sistema de quests
        if (QuestManager.Instance != null)
        {
            EventManager.TriggerQuestCompleted(this);
        }
        
        UnregisterQuestListeners();
    }
    
    public void FailQuest()
    {
        if (status != QuestStatus.Active) return;
        
        status = QuestStatus.Failed;
        
        Debug.Log($"Quest falhada: {questName}");
        
        OnQuestFailed?.Invoke(this);
        
        // Notificar sistema de quests
        if (EventManager.Instance != null)
        {
            EventManager.TriggerQuestCompleted(this); // Usar método existente
        }
        
        UnregisterQuestListeners();
    }
    
    public void UpdateTimer(float deltaTime)
    {
        if (!hasTimeLimit || status != QuestStatus.Active) return;
        
        timeRemaining -= deltaTime;
        
        if (timeRemaining <= 0)
        {
            FailQuest();
        }
    }
    
    private void RegisterQuestListeners()
    {
        switch (questType)
        {
            case QuestType.Kill:
                EventManager.OnEnemyKilled += HandleEnemyKilled;
                break;
            case QuestType.Collect:
                EventManager.OnItemCollected += HandleItemCollected;
                break;
            case QuestType.Deliver:
                EventManager.OnItemDelivered += HandleItemDelivered;
                break;
            case QuestType.Interact:
                EventManager.OnObjectInteracted += HandleObjectInteracted;
                break;
        }
    }

    private void UnregisterQuestListeners()
    {
        switch (questType)
        {
            case QuestType.Kill:
                EventManager.OnEnemyKilled -= HandleEnemyKilled;
                break;
            case QuestType.Collect:
                EventManager.OnItemCollected -= HandleItemCollected;
                break;
            case QuestType.Deliver:
                EventManager.OnItemDelivered -= HandleItemDelivered;
                break;
            case QuestType.Interact:
                EventManager.OnObjectInteracted -= HandleObjectInteracted;
                break;
        }
    }
    
    private void HandleEnemyKilled(string enemyId)
    {
        if (questType == QuestType.Kill && (string.IsNullOrEmpty(targetObjectId) || targetObjectId == enemyId))
        {
            UpdateProgress();
        }
    }
    
    private void HandleItemCollected(string itemId)
    {
        if (questType == QuestType.Collect && targetObjectId == itemId)
        {
            UpdateProgress();
        }
    }
    
    private void HandleItemDelivered(string itemId, string npcId)
    {
        if (questType == QuestType.Deliver && targetObjectId == itemId)
        {
            UpdateProgress();
        }
    }
    
    private void HandleObjectInteracted(string objectId)
    {
        if (questType == QuestType.Interact && targetObjectId == objectId)
        {
            UpdateProgress();
        }
    }
    
    public bool CanStart()
    {
        if (status != QuestStatus.NotStarted) return false;
        
        // Verificar nível requerido
        if (PlayerStats.Instance != null && PlayerStats.Instance.Level < requiredLevel)
        {
            return false;
        }
        
        // Verificar pré-requisitos
        if (prerequisiteQuestIds.Count > 0 && QuestManager.Instance != null)
        {
            foreach (string prereqId in prerequisiteQuestIds)
            {
                if (!QuestManager.Instance.IsQuestCompleted(prereqId))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    public bool IsCompleted()
    {
        return status == QuestStatus.Completed || status == QuestStatus.TurnedIn;
    }
    
    public bool IsActive()
    {
        return status == QuestStatus.Active;
    }
    
    public bool IsFailed()
    {
        return status == QuestStatus.Failed;
    }
    
    public float GetProgressPercentage()
    {
        if (targetAmount <= 0) return 0f;
        return (float)currentProgress / targetAmount;
    }
    
    public string GetProgressText()
    {
        return $"{currentProgress}/{targetAmount}";
    }
    
    public float GetTimeRemaining()
    {
        return hasTimeLimit ? timeRemaining : -1f;
    }
    
    public string GetTimeRemainingText()
    {
        if (!hasTimeLimit) return "";
        
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        return $"{minutes:00}:{seconds:00}";
    }
    
    public void GiveRewards()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddExperience(experienceReward);
            PlayerStats.Instance.AddGold(goldReward);
        }
        
        // Para recompensas de itens, seria necessário um sistema de inventário
        // Como não temos InventoryManager, deixamos comentado
        /*
        if (InventoryManager.Instance != null)
        {
            foreach (ItemReward reward in itemRewards)
            {
                InventoryManager.Instance.AddItem(reward.itemId, reward.quantity);
            }
        }
        */
        
        Debug.Log($"Recompensas da quest {questName} entregues: {experienceReward} XP, {goldReward} ouro");
    }
    
    public void SetProgress(int newProgress)
    {
        if (status != QuestStatus.Active) return;
        
        int previousProgress = currentProgress;
        currentProgress = Mathf.Clamp(newProgress, 0, targetAmount);
        
        OnProgressUpdated?.Invoke(this, currentProgress - previousProgress);
        
        if (currentProgress >= targetAmount)
        {
            CompleteQuest();
        }
    }
    
    public void ResetQuest()
    {
        if (!isRepeatable) return;
        
        status = QuestStatus.NotStarted;
        currentProgress = 0;
        timeRemaining = timeLimit;
        
        UnregisterQuestListeners();
    }
    
    public Quest Clone()
    {
        Quest clonedQuest = new Quest();
        
        clonedQuest.questId = this.questId;
        clonedQuest.questName = this.questName;
        clonedQuest.questDescription = this.questDescription;
        clonedQuest.questType = this.questType;
        clonedQuest.status = QuestStatus.NotStarted;
        clonedQuest.targetAmount = this.targetAmount;
        clonedQuest.currentProgress = 0;
        clonedQuest.targetObjectId = this.targetObjectId;
        clonedQuest.targetLocation = this.targetLocation;
        clonedQuest.targetRadius = this.targetRadius;
        clonedQuest.experienceReward = this.experienceReward;
        clonedQuest.goldReward = this.goldReward;
        clonedQuest.itemRewards = new List<ItemReward>(this.itemRewards);
        clonedQuest.nextQuestId = this.nextQuestId;
        clonedQuest.prerequisiteQuestIds = new List<string>(this.prerequisiteQuestIds);
        clonedQuest.hasTimeLimit = this.hasTimeLimit;
        clonedQuest.timeLimit = this.timeLimit;
        clonedQuest.timeRemaining = this.timeLimit;
        clonedQuest.isRepeatable = this.isRepeatable;
        clonedQuest.isHidden = this.isHidden;
        clonedQuest.requiredLevel = this.requiredLevel;
        
        return clonedQuest;
    }
    
    public Dictionary<string, object> ToSaveData()
    {
        Dictionary<string, object> saveData = new Dictionary<string, object>();
        
        saveData["questId"] = questId;
        saveData["status"] = (int)status;
        saveData["currentProgress"] = currentProgress;
        saveData["timeRemaining"] = timeRemaining;
        
        return saveData;
    }
    
    public void LoadFromSaveData(Dictionary<string, object> saveData)
    {
        if (saveData.ContainsKey("status"))
        {
            status = (QuestStatus)(int)saveData["status"];
        }
        
        if (saveData.ContainsKey("currentProgress"))
        {
            currentProgress = (int)saveData["currentProgress"];
        }
        
        if (saveData.ContainsKey("timeRemaining"))
        {
            timeRemaining = (float)saveData["timeRemaining"];
        }
        
        // Se a quest estava ativa, re-registrar listeners
        if (status == QuestStatus.Active)
        {
            RegisterQuestListeners();
        }
    }
}