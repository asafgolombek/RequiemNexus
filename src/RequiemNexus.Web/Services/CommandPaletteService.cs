using System;

namespace RequiemNexus.Web.Services;

public sealed class CommandPaletteService
{
    private bool _isOpen;

    public event Action? OnStateChanged;

    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                OnStateChanged?.Invoke();
            }
        }
    }

    public void Open() => IsOpen = true;

    public void Close() => IsOpen = false;

    public void Toggle() => IsOpen = !IsOpen;
}
