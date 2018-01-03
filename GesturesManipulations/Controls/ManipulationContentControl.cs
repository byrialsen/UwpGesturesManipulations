using System;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Linq;

namespace GesturesManipulations.Controls
{
    [TemplatePart(Name = BorderPartName, Type = typeof(Border))]
    public sealed class ManipulationContentControl : ContentControl
    {
        #region Fields/const/events

        private const string BorderPartName = "PART_Border";

        Border _border;
        TransformGroup _currentTransformations;

        public delegate void Log(string text);
        public event Log LogEvent;

        #endregion Fields/const

        #region DP

        /// <summary>
        /// Maximum zoom factor
        /// </summary>
        public double MaxZoomFactor
        {
            get { return (double)GetValue(MaxZoomFactorProperty); }
            set { SetValue(MaxZoomFactorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxZoomFactor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxZoomFactorProperty =
            DependencyProperty.Register("MaxZoomFactor", typeof(double), typeof(ManipulationContentControl), new PropertyMetadata(5.0));

        /// <summary>
        /// Minimum zoom factor
        /// </summary>
        public double MinZoomFactor
        {
            get { return (double)GetValue(MinZoomFactorProperty); }
            set { SetValue(MinZoomFactorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinZoomFactor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinZoomFactorProperty =
            DependencyProperty.Register("MinZoomFactor", typeof(double), typeof(ManipulationContentControl), new PropertyMetadata(0.2));

        /// <summary>
        /// Is scale active
        /// </summary>
        public bool HandleScale
        {
            get { return (bool)GetValue(HandleScaleProperty); }
            set { SetValue(HandleScaleProperty, value); }
        }

        public static readonly DependencyProperty HandleScaleProperty = DependencyProperty.Register(
            "HandleScale",
            typeof(bool),
            typeof(ManipulationContentControl),
            new PropertyMetadata(true));

        /// <summary>
        /// Is rotation active
        /// </summary>
        public bool HandleRotation
        {
            get { return (bool)GetValue(HandleRotationProperty); }
            set { SetValue(HandleRotationProperty, value); }
        }

        public static readonly DependencyProperty HandleRotationProperty = DependencyProperty.Register(
            "HandleRotation",
            typeof(bool),
            typeof(ManipulationContentControl),
            new PropertyMetadata(true));

        /// <summary>
        /// Is panning active
        /// </summary>
        public bool HandlePan
        {
            get { return (bool)GetValue(HandlePanProperty); }
            set { SetValue(HandlePanProperty, value); }
        }

        public static readonly DependencyProperty HandlePanProperty = DependencyProperty.Register(
            "HandlePan",
            typeof(bool),
            typeof(ManipulationContentControl),
            new PropertyMetadata(true));

        #endregion DP

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ManipulationContentControl()
        {
            this.DefaultStyleKey = typeof(ManipulationContentControl);

            // design mode
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                // do something to please designer
            }
        }

        #endregion Constructor

        #region Methods

        protected override void OnApplyTemplate()
        {
            _border = (Border)GetTemplateChild(BorderPartName);

            if (_border == null)
            {
                throw new NullReferenceException();
            }

            SetupManipulation();
            InitializeTransformation();

            _border.ManipulationDelta += OnManipulationDelta;
            _border.ManipulationCompleted += OnManipulationCompleted;
            _border.DoubleTapped += OnDoubleTapped;
            _border.PointerWheelChanged += OnPointerWheelChanged;

            this.SizeChanged += OnControlSizeChanged;

            base.OnApplyTemplate();
        }

        /// <summary>
        /// Controls SizeChanged event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DoLog($"SizeChanged {e.NewSize.ToVector2()}");

            Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }

        /// <summary>
        /// Handles touchpad and mouse rotation/zoom (ok)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            DoLog("PointerWheelChanged");

            // get keystates
            bool shift = (e.KeyModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;
            bool ctrl = (e.KeyModifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;

            if (ctrl)
            {
                // get new transformation
                var newTransformation = FlattenTransformationGroup();

                var newDeltaTransform = newTransformation.Children.OfType<CompositeTransform>().FirstOrDefault();
                if (newDeltaTransform == null)
                {
                    return;
                }

                // get center in respect to existing transformations
                Point center = _currentTransformations.TransformPoint(e.GetCurrentPoint(_border).Position);
                newDeltaTransform.CenterX = center.X;
                newDeltaTransform.CenterY = center.Y;

                // get scroll info
                var deltaScroll = (-1 * e.GetCurrentPoint(_border).Properties.MouseWheelDelta) > 0 ? 0.9 : 1.1;

                // handle rotation
                if (shift)
                {
                    // rotation
                    if (deltaScroll > 1)
                    {
                        newDeltaTransform.Rotation = -15.0;
                        DoLog($"delta > 0 -> {newDeltaTransform.Rotation}");
                    }
                    else if (deltaScroll < 1)
                    {
                        newDeltaTransform.Rotation = 15.0;
                        DoLog($"delta < 0 -> {newDeltaTransform.Rotation}");
                    }
                }
                // handle scale
                else
                {
                    newDeltaTransform.ScaleX = newDeltaTransform.ScaleY = deltaScroll;
                    if (!CheckScaleTresshold(newTransformation))
                    {
                        newDeltaTransform.ScaleX = newDeltaTransform.ScaleY = 1.0;
                    }
                }

                // update with new transformations
                _border.RenderTransform = _currentTransformations = newTransformation;
            }
        }

        /// <summary>
        /// Handles touchpad/tourch/mouse dbl click (ok)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //DoLog("Double tapped");

            // get new transformation
            var newTransformation = FlattenTransformationGroup();

            var newDeltaTransform = newTransformation.Children.OfType<CompositeTransform>().FirstOrDefault();
            if (newDeltaTransform == null)
            {
                return;
            }

            // get center in respect to existing transformations
            Point center = _currentTransformations.TransformPoint(e.GetPosition(_border));
            newDeltaTransform.CenterX = center.X;
            newDeltaTransform.CenterY = center.Y;

            // reset 
            double existingScale = newTransformation.Value.GetScale();

            // handle scaling
            if (existingScale < 1.1)
            {
                // zoom in
                newDeltaTransform.ScaleX = newDeltaTransform.ScaleY = 3.0;
            }
            else
            {
                // reset zoom
                newDeltaTransform.ScaleX = newDeltaTransform.ScaleY = (1 / existingScale);
            }

            // apply transformations
            _border.RenderTransform = _currentTransformations = newTransformation;
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            DoLog("Manipulation completed");
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //DoLog("Manipulation delta");

            // get new transformation
            var newTransformation = FlattenTransformationGroup();

            var newDeltaTransform = newTransformation.Children.OfType<CompositeTransform>().FirstOrDefault();
            if (newDeltaTransform == null)
            {
                return;
            }

            // get center in respect to existing transformations
            Point center = _currentTransformations.TransformPoint(e.Position);
            newDeltaTransform.CenterX = center.X;
            newDeltaTransform.CenterY = center.Y;

            // pan
            newDeltaTransform.TranslateX = e.Delta.Translation.X;
            newDeltaTransform.TranslateY = e.Delta.Translation.Y;
            //ConstrainPan();

            // rotation
            newDeltaTransform.Rotation = e.Delta.Rotation;

            // scale
            newDeltaTransform.ScaleX = newDeltaTransform.ScaleY = e.Delta.Scale;
            //TressholdScale();

            // apply transformations
            _border.RenderTransform = _currentTransformations = newTransformation;
        }

        /// <summary>
        /// Store existing transformation in matrix 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private TransformGroup FlattenTransformationGroup()
        {
            //DoLog($"");

            TransformGroup newTransformGroup = new TransformGroup();
            newTransformGroup.Children.Add(new MatrixTransform() { Matrix = _currentTransformations.Value });
            newTransformGroup.Children.Add(new CompositeTransform());

            return newTransformGroup;
        }

        /// <summary>
        /// Initialize transformations
        /// </summary>
        public void InitializeTransformation()
        {
            TransformGroup newTransformGroup = new TransformGroup();
            newTransformGroup.Children.Add(new MatrixTransform() { Matrix = Matrix.Identity });
            newTransformGroup.Children.Add(new CompositeTransform());

            _currentTransformations = newTransformGroup;
        }

        /// <summary>
        /// Setup Manipulation objects
        /// </summary>
        private void SetupManipulation()
        {
            _border.IsTapEnabled = true;
            _border.IsDoubleTapEnabled = true;

            _border.ManipulationMode = ManipulationModes.None;

            if (HandlePan)
            {
                _border.ManipulationMode = _border.ManipulationMode | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            }

            if (HandleRotation)
            {
                _border.ManipulationMode = _border.ManipulationMode | ManipulationModes.Rotate;
            }

            if (HandleScale)
            {
                _border.ManipulationMode = _border.ManipulationMode | ManipulationModes.Scale;
            }
        }

        private bool CheckScaleTresshold(TransformGroup newTransformations)
        {
            return (newTransformations.Value.GetScale() <= MaxZoomFactor && newTransformations.Value.GetScale() >= MinZoomFactor);
        }

        /// <summary>
        /// Logging methods
        /// </summary>
        /// <param name="text"></param>
        private void DoLog(string text)
        {
            LogEvent?.Invoke(text);
        }

        #endregion Methods
    }
}
