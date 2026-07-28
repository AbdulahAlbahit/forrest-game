using UnityEngine;

/// <summary>
/// Bulut'un atolye panelini Efe'nin kamyon secim kapisina baglar.
/// UI yalnizca tarif secer; odun hesabi, Mert'in gorsel uretimi ve kamyonun
/// serbest birakilmasi EfeWorkshopSelectionController tarafinda kalir.
/// </summary>
public sealed class EfeWorkshopPanelBridge : MonoBehaviour
{
    [Header("Baglantilar")]
    [SerializeField] private EfeWorkshopSelectionController selectionController;
    [SerializeField] private WorkshopPanelController workshopPanel;

    private void OnEnable()
    {
        EfeWorkshopSelectionController.OnSelectionRequested += HandleSelectionRequested;
        EfeWorkshopSelectionController.OnRecipeSelected += HandleRecipeSelected;
        EfeWorkshopSelectionController.OnSelectionRejected += HandleSelectionRejected;
        ResolveReferences();
        SubscribePanel();
    }

    private void OnDisable()
    {
        EfeWorkshopSelectionController.OnSelectionRequested -= HandleSelectionRequested;
        EfeWorkshopSelectionController.OnRecipeSelected -= HandleRecipeSelected;
        EfeWorkshopSelectionController.OnSelectionRejected -= HandleSelectionRejected;
        UnsubscribePanel();
    }

    private void HandleSelectionRequested(TruckRouteController truck, int deliveredWood)
    {
        if (!ResolveReferences())
        {
            Debug.LogWarning(
                "[EfeWorkshopPanelBridge] Atolye paneli bulunamadi; 1/2 klavye yedegi kullanilabilir.",
                this);
            return;
        }

        SubscribePanel();
        workshopPanel.SetCloseButtonInteractable(false);
        workshopPanel.ShowExternalQueue(selectionController.QueuedRecipes, deliveredWood);
        workshopPanel.OpenPanel();

        Debug.Log(
            $"[EfeWorkshopPanelBridge] Atolye paneli acildi. Teslim edilen odun: {deliveredWood}.",
            this);
    }

    private void HandleProductionRequested(RecipeConfig recipe)
    {
        if (!ResolveReferences())
        {
            return;
        }

        // Kamyon bekleyen bir secim yoksa Uret butonu Efe'nin onay akisini degil,
        // tarif tiklaninca zaten kuyruga giren WorkshopManager akisini kullanir.
        if (!selectionController.IsAwaitingSelection)
        {
            WorkshopManager.Instance?.TryStartProduction(recipe, 1);
            return;
        }

        if (selectionController.ConfirmSelection())
        {
            workshopPanel.ClearExternalQueueDisplay();
            workshopPanel.CloseImmediately();
        }
    }

    private void HandleRecipeChosen(RecipeConfig recipe)
    {
        if (!ResolveReferences())
        {
            return;
        }

        // Kamyon atolyede secim bekliyorsa Efe'nin mevcut secim akisi devam etsin.
        if (selectionController.IsAwaitingSelection)
        {
            selectionController.SelectRecipe(recipe);
            return;
        }

        // Bekleyen kamyon yoksa tarif dogrudan WorkshopManager kuyruguna girsin;
        // PlayerData.WorkshopRawWood yeterliyse uretir, degilse kalici kuyrukta bekler.
        WorkshopManager.Instance?.TryStartProduction(recipe, 1);
    }

    private void HandleRecipeSelected(RecipeConfig recipe, int amount, int remainingWood)
    {
        if (workshopPanel == null)
        {
            return;
        }

        workshopPanel.ShowExternalQueue(selectionController.QueuedRecipes, remainingWood);
        workshopPanel.SetCloseButtonInteractable(false);
        NotificationUIController.Instance?.ShowNotification(
            $"{recipe.ItemID} kuyruğa eklendi. Seçimin bitince Üret'e bas.");
    }

    private void HandleCloseRequested()
    {
        if (!ResolveReferences())
        {
            return;
        }

        // Kamyon bekleyen bir secim varsa panel kapanmasin, Uret'e yonlendir.
        if (selectionController.IsAwaitingSelection)
        {
            NotificationUIController.Instance?.ShowNotification(
                "Atölye seçimini tamamlamak için Üret düğmesine bas.");
            return;
        }

        // Bekleyen secim yoksa panel normal sekilde kapanabilir.
        workshopPanel.CloseImmediately();
    }

    private void HandleSelectionRejected(string reason)
    {
        NotificationUIController.Instance?.ShowNotification(reason);
        Debug.LogWarning($"[EfeWorkshopPanelBridge] Secim kabul edilmedi: {reason}", this);
    }

    private bool ResolveReferences()
    {
        if (selectionController == null)
        {
            selectionController = EfeWorkshopSelectionController.Instance;
        }

        if (workshopPanel == null)
        {
            workshopPanel = FindFirstObjectByType<WorkshopPanelController>(FindObjectsInactive.Include);
        }

        return selectionController != null && workshopPanel != null;
    }

    private void SubscribePanel()
    {
        if (workshopPanel == null)
        {
            return;
        }

        workshopPanel.ProductionRequested -= HandleProductionRequested;
        workshopPanel.ProductionRequested += HandleProductionRequested;
        workshopPanel.RecipeChosen -= HandleRecipeChosen;
        workshopPanel.RecipeChosen += HandleRecipeChosen;
        workshopPanel.CloseRequested -= HandleCloseRequested;
        workshopPanel.CloseRequested += HandleCloseRequested;
    }

    private void UnsubscribePanel()
    {
        if (workshopPanel != null)
        {
            workshopPanel.ProductionRequested -= HandleProductionRequested;
            workshopPanel.RecipeChosen -= HandleRecipeChosen;
            workshopPanel.CloseRequested -= HandleCloseRequested;
        }
    }
}
