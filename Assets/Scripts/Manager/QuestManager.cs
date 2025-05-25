using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CurrentQuestInfo
{
    public QuestData Data;
    public int StepIndex;
}
[System.Serializable]
public class QuestStatus
{
    public string QuestID;
    public int StepIndex;
}

public class QuestManager : MonoBehaviour
{
    [Header("퀘스트 데이터베이스")]
    [SerializeField] public QuestDB m_questDB;

    public Dictionary<string, string> m_questStates = new();
    public Dictionary<string, int> m_currentSteps = new();
    public Dictionary<string, bool> m_questFlags = new();
    [Header("현재 진행 중인 퀘스트 (읽기 전용)")]
    [SerializeField] private List<QuestStatus> m_debugQuestList = new();

    public void StartQuest(string questID)
    {
        if (!m_questStates.ContainsKey(questID))
        {
            m_questStates[questID] = "Started";
            m_currentSteps[questID] = 0;
            Debug.Log($"[퀘스트 시작] {questID}");
            SetQuestFlag($"{questID}_Step0_Start", true);
            UpdateQuestInspectorList();

        }
    }

    public void TryCompleteStep(string questID)
    {
        var step = GetCurrentStep(questID);
        if (step == null) return;

        switch (step.m_stepType)
        {
            case QuestStepType.Visit:
                string currentMap = GManager.Instance.currentMapGroup?.name;
                string targetMap = step.m_targetMapId;

                Debug.Log($"[Quest Visit] 현재 맵: {currentMap}, 타겟 맵: {targetMap}");

                if (currentMap == targetMap)
                {
                    Debug.Log($"[Quest Visit] 맵 일치 - 스텝 완료 처리됨");
                    AdvanceStep(questID);
                }
                else
                {
                    Debug.Log($"[Quest Visit] 맵 불일치 - 진행 조건 미달");
                }
                break;

            case QuestStepType.Gather:
            case QuestStepType.Craft:
                if (GManager.Instance.IsinvenManager == null ||
                    GManager.Instance.IsinvenManager.IsInventoryData == null ||
                    step.m_targetItem == null)
                {
                    Debug.LogError("[QuestManager] 인벤토리 매니저나 데이터가 null입니다.");
                    return;
                }

                bool hasItem = GManager.Instance.IsinvenManager.IsInventoryData.HasItem(step.m_targetItem, step.m_requiredAmount);
                if (hasItem)
                    AdvanceStep(questID);
                break;

            case QuestStepType.Talk:
            case QuestStepType.Deliver:
                // Deliver는 End 노드에서 따로 처리
                break;
        }
    }

    public void TryTalkToNPC(string npcID)
    {
        foreach (var kvp in m_questStates)
        {
            if (kvp.Value == "Started")
            {
                string questID = kvp.Key;
                var step = GetCurrentStep(questID);

                Debug.Log($"[TryTalkToNPC] 퀘스트 {questID} 현재 Step: {step?.m_stepType}, 타겟: {step?.m_targetNpcId}");

                if (step != null && step.m_stepType == QuestStepType.Talk && step.m_targetNpcId == npcID)
                {
                    AdvanceStep(questID);
                    Debug.Log($"[퀘스트 진행] {questID} - {npcID}와 대화하여 스텝 완료");
                    break;
                }
            }
        }
    }
    public void TryVisit()
    {
        //  Dictionary의 키 리스트를 미리 복사
        var questKeys = new List<string>(m_questStates.Keys);

        foreach (var questID in questKeys)
        {
            if (m_questStates[questID] == "Started")
            {
                var step = GetCurrentStep(questID);
                if (step != null && step.m_stepType == QuestStepType.Visit)
                {
                    TryCompleteStep(questID);
                }
            }
        }
    }
    public List<string> GetAllStartedQuests()
    {
        List<string> result = new();
        foreach (var kvp in m_questStates)
        {
            if (kvp.Value == "Started")
                result.Add(kvp.Key);
        }
        return result;
    }
    public bool CheckDeliverCondition(string questID)
    {
        var step = GetCurrentStep(questID);
        if (step == null || step.m_stepType != QuestStepType.Deliver)
            return false;

        return GManager.Instance.IsinvenManager.IsInventoryData.HasItem(step.m_targetItem, step.m_requiredAmount);
    }
    public void CompleteDeliverStep(string questID)
    {
        var step = GetCurrentStep(questID);
        if (step == null || step.m_stepType != QuestStepType.Deliver)
            return;

        if (GManager.Instance.IsinvenManager.IsInventoryData.HasItem(step.m_targetItem, step.m_requiredAmount))
        {
            GManager.Instance.IsinvenManager.RemoveItem(step.m_targetItem, step.m_requiredAmount);
            AdvanceStep(questID);
            Debug.Log($"[QuestManager] Deliver Step 완료 처리: {questID}");
        }
    }


    private void AdvanceStep(string questID)
    {
        var data = GetQuestData(questID);
        if (data == null) return;

        int step = GetCurrentStepIndex(questID);
        if (step + 1 < data.m_questSteps.Count)
        {
            m_currentSteps[questID] = step + 1;

            Debug.Log($"[퀘스트 진행] {questID} - Step {step} 완료 → Step {step + 1} 시작");

            SetQuestFlag($"{questID}_Step{step}_Start", false);
            SetQuestFlag($"{questID}_Step{step}_Clear", true);
            SetQuestFlag($"{questID}_Step{step + 1}_Start", true);
        }
        else
        {
            Debug.Log($"[퀘스트 진행] {questID} - 마지막 Step {step} 완료 → 퀘스트 완료 예정");

            //마지막 스텝에 다음 퀘스트가 지정돼 있으면 시작
            var currentStep = data.m_questSteps[step];
            if (!string.IsNullOrEmpty(currentStep.m_nextQuestID))
            {
                Debug.Log($"[퀘스트 진행] {questID} - 다음 퀘스트 자동 시작: {currentStep.m_nextQuestID}");
                StartQuest(currentStep.m_nextQuestID);
            }

            CompleteQuest(questID);
        }

        UpdateQuestInspectorList(); // 인스펙터 갱신용
    }


    public void CompleteQuest(string questID)
    {
        m_questStates[questID] = "Complete";

        Debug.Log($"[퀘스트 완료] {questID}");

        var data = GetQuestData(questID);
        if (data != null)
        {
            foreach (var reward in data.m_rewardItems)
            {
                InventoryManager.Instance.AddItem(reward.m_item, reward.m_amount);
            }
        }

        SetQuestFlag($"{questID}_Complete", true);

        UpdateQuestInspectorList();
    }

    public QuestData GetQuestData(string questID)
    {
        return m_questDB.GetQuestById(questID);
    }

    public int GetCurrentStepIndex(string questID)
    {
        return m_currentSteps.TryGetValue(questID, out var step) ? step : 0;
    }

    public QuestStep GetCurrentStep(string questID)
    {
        var data = GetQuestData(questID);
        if (data == null) return null;

        int index = GetCurrentStepIndex(questID);
        if (index >= 0 && index < data.m_questSteps.Count)
            return data.m_questSteps[index];

        return null;
    }

    public void SetQuestFlag(string flagName, bool value)
    {
        m_questFlags[flagName] = value;
        Debug.Log($"[퀘스트 플래그] {flagName} = {value}");
    }

    public bool GetQuestFlag(string flagName)
    {
        return m_questFlags.TryGetValue(flagName, out var result) && result;
    }

    public string GetQuestState(string questID)
    {
        return m_questStates.TryGetValue(questID, out var state) ? state : "NotStarted";
    }
    //현재 메인퀘스트 정보
    public CurrentQuestInfo GetCurrentMainQuestInfo()
    {
        foreach (var kvp in m_questStates)
        {
            if (kvp.Value == "Started")
            {
                var data = GetQuestData(kvp.Key);
                if (data != null && data.m_questType == QuestType.Main)
                {
                    return new CurrentQuestInfo
                    {
                        Data = data,
                        StepIndex = GetCurrentStepIndex(kvp.Key)
                    };
                }
            }
        }
        return null;
    }
    //현재 서브 퀘스트 정보
    public List<CurrentQuestInfo> GetCurrentSubQuestInfos()
    {
        var result = new List<CurrentQuestInfo>();
        foreach (var kvp in m_questStates)
        {
            if (kvp.Value == "Started")
            {
                var data = GetQuestData(kvp.Key);
                if (data != null && data.m_questType == QuestType.Sub)
                {
                    result.Add(new CurrentQuestInfo
                    {
                        Data = data,
                        StepIndex = GetCurrentStepIndex(kvp.Key)
                    });
                }
            }
        }
        return result;
    }
    private void UpdateQuestInspectorList()
    {
        m_debugQuestList.Clear();

        foreach (var kvp in m_questStates)
        {
            if (kvp.Value == "Started")
            {
                m_debugQuestList.Add(new QuestStatus
                {
                    QuestID = kvp.Key,
                    StepIndex = GetCurrentStepIndex(kvp.Key)
                });
            }
        }
    }
    public void TryCompleteStepAll()
    {
        foreach (var kvp in m_questStates)
        {
            if (kvp.Value == "Started")
            {
                TryCompleteStep(kvp.Key);
            }
        }
    }


}
