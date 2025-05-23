using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    [SerializeField] private InventoryData InventoryData;
    public InventoryData IsInventoryData => InventoryData; // 읽기 전용 속성으로 외부 접근 허용
    
    [SerializeField] private int inventorySize = 24;

    private void Awake()
    {
        if (InventoryData == null || InventoryData.slots == null || InventoryData.slots.Length != inventorySize)
        {
            InventoryData = new InventoryData(inventorySize);
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        InventoryData.AddItem(item, amount);
        GManager.Instance.IsInventoryUI.UpdateUI(); // 추가
    }

    public void RemoveItem(ItemData item, int amount = 1)
    {
        InventoryData.RemoveItem(item, amount);
        GManager.Instance.IsInventoryUI.UpdateUI(); // 추가
    }

    public void CheckInvenItem(ItemData item, int amount)
    {
        if (!InventoryData.HasItem(item, amount))
        {
            return;
        }

    }
    public void CheckInvenSpace(ItemData item, int amount)
    {
        if (!InventoryData.HasSpaceForItem(item, amount))
        {
            Debug.LogWarning("[인벤토리 공간 부족] 아이템을 추가할 수 없습니다.");
            return;
        }
    }
    public void ConsumeItem(ItemData item, int amount)
    {
        CheckInvenItem(item, amount); // 소모 전에 반드시 확인
        InventoryData.RemoveItem(item, amount);
        GManager.Instance.IsInventoryUI.UpdateUI();
    }


}

