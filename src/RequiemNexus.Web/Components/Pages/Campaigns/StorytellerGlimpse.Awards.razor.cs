using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class StorytellerGlimpse
{
    private void HandleDragStart(DragEventArgs e)
    {
        e.DataTransfer.EffectAllowed = "move";
    }

    private void HandleDragEnter(int id)
    {
        _activeDropTargetId = id;
    }

    private void HandleDragLeave()
    {
        _activeDropTargetId = null;
    }

    private async Task HandleDrop(int characterId)
    {
        _activeDropTargetId = null;
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCharacterAsync(Id, characterId, "Drag to award Beat", _currentUserId!);
            ToastService.Show("Success", "Beat awarded via drag-drop.", ToastType.Success);
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardBeat(int characterId)
    {
        if (string.IsNullOrWhiteSpace(_beatReasons[characterId]) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCharacterAsync(Id, characterId, _beatReasons[characterId], _currentUserId!);
            _beatReasons[characterId] = string.Empty;
            _feedbackMessages[characterId] = "Beat awarded.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardXp(int characterId)
    {
        if (_xpAmounts[characterId] <= 0 || string.IsNullOrWhiteSpace(_xpReasons[characterId]) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardXpToCharacterAsync(Id, characterId, _xpAmounts[characterId], _xpReasons[characterId], _currentUserId!);
            _xpReasons[characterId] = string.Empty;
            _feedbackMessages[characterId] = "XP awarded.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardCoterieBeat()
    {
        if (string.IsNullOrWhiteSpace(_coteReason) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCampaignAsync(Id, _coteReason, _currentUserId!);
            _coteReason = string.Empty;
            _coteMessage = "Beat awarded to all.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }
}
