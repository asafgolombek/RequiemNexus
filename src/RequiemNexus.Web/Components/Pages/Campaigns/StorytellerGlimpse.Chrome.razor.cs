using Microsoft.AspNetCore.Components.Web;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class StorytellerGlimpse
{
    private void TogglePinNpc(int id)
    {
        if (_pinnedNpcIds.Contains(id))
        {
            _pinnedNpcIds.Remove(id);
        }
        else
        {
            _pinnedNpcIds.Add(id);
        }
    }

    private void OpenLineageEditor(int characterId)
    {
        _lineageEditCharacterId = characterId;
        _lineageModalOpen = true;
    }

    private async Task OnLineageSavedAsync()
    {
        await LoadVitals();
    }

    private void SelectGlimpseTab(string tab) => _glimpseTab = tab;

    private void HandleGlimpseTabKeydown(KeyboardEventArgs e)
    {
        string[] tabs = ["overview", "social", "approvals", "chronicle"];
        int i = Array.IndexOf(tabs, _glimpseTab);
        if (i < 0)
        {
            return;
        }

        if (e.Key == "ArrowRight" || e.Key == "ArrowDown")
        {
            SelectGlimpseTab(tabs[(i + 1) % tabs.Length]);
        }
        else if (e.Key == "ArrowLeft" || e.Key == "ArrowUp")
        {
            SelectGlimpseTab(tabs[(i - 1 + tabs.Length) % tabs.Length]);
        }
        else if (e.Key == "Home")
        {
            SelectGlimpseTab(tabs[0]);
        }
        else if (e.Key == "End")
        {
            SelectGlimpseTab(tabs[^1]);
        }
    }
}
