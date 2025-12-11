using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components;

public partial class ImageCompare
{
    [Inject] public IJSRuntime JS { get; set; } = null!;

    [Parameter] public string BeforeImage { get; set; } = "";
    [Parameter] public string AfterImage { get; set; } = "";
    [Parameter] public string BeforeLabel { get; set; } = "Before";
    [Parameter] public string AfterLabel { get; set; } = "After";
    [Parameter] public int InitialPosition { get; set; } = 50;
    [Parameter] public int InitialAngle { get; set; } = 0;

    private ElementReference _compareElement;
    private string CurrentBeforeImage { get; set; } = "";
    private string CurrentAfterImage { get; set; } = "";
    private int SliderPosition { get; set; } = 50;
    private int HandleAngle { get; set; } = 0;
    private bool _isDragging = false;
    private bool _isSwapped = false;
    private bool _isVertical = true;  // true = TOP-BOTTOM, false = LEFT-RIGHT
    private bool _isReversed = false;

    protected override void OnInitialized()
    {
        CurrentBeforeImage = BeforeImage;
        CurrentAfterImage = AfterImage;
        SliderPosition = InitialPosition;
        HandleAngle = InitialAngle;
    }

    private void OnSliderInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value))
        {
            SliderPosition = Math.Clamp(value, 0, 100);
        }
    }

    private void OnAngleInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value))
        {
            HandleAngle = Math.Clamp(value, -45, 45);
        }
    }

    private void SetPosition(int position)
    {
        SliderPosition = Math.Clamp(position, 0, 100);
    }

    private void SwapImages()
    {
        _isSwapped = !_isSwapped;
        (CurrentBeforeImage, CurrentAfterImage) = (CurrentAfterImage, CurrentBeforeImage);
        SliderPosition = 50;
    }

    private void ToggleOrientation()
    {
        _isVertical = !_isVertical;
        SliderPosition = 50;
        HandleAngle = 0;
    }

    private void ToggleDirection()
    {
        _isReversed = !_isReversed;
    }

    private void ResetAngle()
    {
        HandleAngle = 0;
    }

    private string GetClipPath()
    {
        double pos = SliderPosition;

        if (HandleAngle == 0)
        {
            // Straight cut
            if (_isVertical)
            {
                return _isReversed
                    ? $"inset(0 0 0 {pos}%)"
                    : $"inset(0 {100 - pos}% 0 0)";
            }
            else
            {
                return _isReversed
                    ? $"inset(0 0 {pos}% 0)"
                    : $"inset({pos}% 0 0 0)";
            }
        }

        // Angled cut with polygon
        double angleRad = HandleAngle * Math.PI / 180.0;
        double tanAngle = Math.Tan(angleRad);

        if (_isVertical)
        {
            // Vertical orientation (left-right cut)
            // Flip offset để line nghiêng đúng chiều
            double offset = tanAngle * 50;
            double topX = Math.Clamp(pos + offset, 0, 100);      // Đảo dấu
            double bottomX = Math.Clamp(pos - offset, 0, 100);   // Đảo dấu

            if (_isReversed)
            {
                return $"polygon(0 0, {topX}% 0, {bottomX}% 100%, 0 100%)";
            }
            else
            {
                return $"polygon({topX}% 0, 100% 0, 100% 100%, {bottomX}% 100%)";
            }
        }
        else
        {
            // Horizontal orientation (top-bottom cut)
            // Flip offset để line nghiêng đúng chiều
            double offset = tanAngle * 50;
            double leftY = Math.Clamp(pos + offset, 0, 100);     // Đảo dấu
            double rightY = Math.Clamp(pos - offset, 0, 100);    // Đảo dấu

            if (_isReversed)
            {
                return $"polygon(0 0, 100% 0, 100% {rightY}%, 0 {leftY}%)";
            }
            else
            {
                return $"polygon(0 {leftY}%, 100% {rightY}%, 100% 100%, 0 100%)";
            }
        }
    }

    private string GetHandlePosition()
    {
        return _isVertical ? $"left: {SliderPosition}%" : $"top: {SliderPosition}%";
    }

    private string GetHandleRotation()
    {
        if (_isVertical)
        {
            return $"translateX(-50%) rotate({HandleAngle}deg)";
        }
        else
        {
            return $"translateY(-50%) rotate({90 + HandleAngle}deg)";
        }
    }

    private async Task StartDrag(MouseEventArgs e)
    {
        _isDragging = true;
        await UpdatePositionFromMouse(e);
    }

    private void StartDrag(TouchEventArgs e)
    {
        _isDragging = true;
    }

    private async Task OnDrag(MouseEventArgs e)
    {
        if (_isDragging)
        {
            await UpdatePositionFromMouse(e);
        }
    }

    private async Task OnDragTouch(TouchEventArgs e)
    {

        if (_isDragging && e.Touches.Length > 0)
        {
            try
            {
                var position = await JS.InvokeAsync<int>("getPercentagePosition", _compareElement, e.Touches[0].ClientX);
                SliderPosition = Math.Clamp(position, 0, 100);
                StateHasChanged();
            }
            catch
            {
                //ignore
            }
        }
    }

    private void EndDrag(MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void EndDrag(TouchEventArgs e)
    {
        _isDragging = false;
    }

    private async Task UpdatePositionFromMouse(MouseEventArgs e)
    {
        try
        {
            var position = await JS.InvokeAsync<int>("getPercentagePosition", _compareElement, e.ClientX);
            SliderPosition = Math.Clamp(position, 0, 100);
            StateHasChanged();
        }
        catch
        {
            // ignored
        }
    }

    public void UpdateImages(string beforeImage, string afterImage)
    {
        BeforeImage = beforeImage;
        AfterImage = afterImage;
        CurrentBeforeImage = _isSwapped ? afterImage : beforeImage;
        CurrentAfterImage = _isSwapped ? beforeImage : afterImage;
        SliderPosition = 50;
        StateHasChanged();
    }
}

