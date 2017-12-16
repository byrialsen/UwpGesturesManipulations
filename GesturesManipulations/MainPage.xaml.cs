using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace GesturesManipulations
{
    /// <summary>
    /// https://stackoverflow.com/questions/36727020/uwp-manipulation-with-rotation-scale-and-pan
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;
        }

        void Viewbox_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // store earlier manipulations
            previousTransform.Matrix = transformGroup.Value;

            // init center
            var center = previousTransform.TransformPoint(e.Position);
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // rotation
            deltaTransform.Rotation = e.Delta.Rotation;

            // Check to see if we are over our scale, then reset to scale to max or min scale

            // scale
            deltaTransform.ScaleX = e.Delta.Scale;
            deltaTransform.ScaleY = e.Delta.Scale;

            // pan
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
        }
    }
}
