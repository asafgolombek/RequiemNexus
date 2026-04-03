// Blazor partial: PDF/JSON export and navigation to advancement for CharacterDetails.
using Microsoft.JSInterop;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private Task GoToAdvancement()
    {
        NavigationManager.NavigateTo($"/character/{Id}/advancement");
        return Task.CompletedTask;
    }

    private async Task ExportJson()
    {
        if (_character == null)
        {
            return;
        }

        _isExporting = true;
        try
        {
            var json = ExportService.ExportCharacterAsJson(_character);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            await JS.InvokeVoidAsync("downloadFileFromBase64", $"{_character.Name}.json", "application/json", base64);
            ToastService.Show("Export complete", "JSON downloaded.", ToastType.Success);
        }
        finally
        {
            _isExporting = false;
        }
    }

    private async Task ExportPdf()
    {
        if (_character == null)
        {
            return;
        }

        _isExporting = true;
        try
        {
            var pdfBytes = ExportService.ExportCharacterAsPdf(_character);
            var base64 = Convert.ToBase64String(pdfBytes);
            await JS.InvokeVoidAsync("downloadFileFromBase64", $"{_character.Name}.pdf", "application/pdf", base64);
            ToastService.Show("Export complete", "PDF downloaded.", ToastType.Success);
        }
        finally
        {
            _isExporting = false;
        }
    }
}
