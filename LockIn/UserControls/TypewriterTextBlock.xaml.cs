using System.Text;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LockIn.UserControls;

[ObservableObject]
public partial class TypewriterTextBlock : TextBlock
{
    private CancellationTokenSource _cts = new CancellationTokenSource();
    public static readonly DependencyProperty TimePerCharacterProperty = DependencyProperty.RegisterAttached("TimePerCharacter", typeof(TimeSpan), typeof(TypewriterTextBlock), new PropertyMetadata(TimeSpan.FromMilliseconds(100))); 
    public static readonly DependencyProperty TextToAnimateProperty = DependencyProperty.RegisterAttached("TextToAnimate", typeof(string), typeof(TypewriterTextBlock), new PropertyMetadata(string.Empty, OnTextChanged));
    
    public static void SetTimePerCharacter(DependencyObject element, TimeSpan value)
    {
        element.SetValue(TimePerCharacterProperty, value);
    }

    public static TimeSpan GetTimePerCharacter(DependencyObject element)
    {
        return (TimeSpan)element.GetValue(TimePerCharacterProperty);
    }

    public static void SetTextToAnimate(DependencyObject element, string value)
    {
        element.SetValue(TextToAnimateProperty, value);
    }

    public static string GetTextToAnimate(DependencyObject element, string value)
    {
        return (string)element.GetValue(TextToAnimateProperty);
    }
    
    public TypewriterTextBlock()
    {
        
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if(d is not TypewriterTextBlock textBlock) return;
        if (e.NewValue == e.OldValue) return;
        textBlock._cts.Cancel();
        textBlock._cts.Dispose();
        textBlock._cts = new CancellationTokenSource();
        _ = textBlock.AnimateText(e.NewValue.ToString() ?? string.Empty, textBlock._cts.Token);
    }

    private async Task AnimateText(string textToAnimate, CancellationToken ct = default)
    {
        if(ct.IsCancellationRequested) return;
        TimeSpan timePerCharacter = (TimeSpan)GetValue(TimePerCharacterProperty);
        StringBuilder bob = new(textToAnimate.Length);
        try
        {
            foreach (char c in textToAnimate)
            {
                if (ct.IsCancellationRequested) return;
                bob.Append(c);
                Text = bob.ToString();
                await Task.Delay(timePerCharacter, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}