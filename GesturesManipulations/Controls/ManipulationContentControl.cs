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
        #region Fields/const

        private const string BorderPartName = "PART_Border";
        Border _border;
        TransformGroup transformGroup;
        MatrixTransform previousTransform;
        CompositeTransform deltaTransform;

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

            InitializeManipulation();
            InitializeAndReset();

            _border.ManipulationDelta += OnManipulationDelta;
            _border.ManipulationCompleted += OnManipulationCompleted;
            _border.DoubleTapped += OnDoubleTapped;
            _border.PointerWheelChanged += OnPointerWheelChanged;

            this.SizeChanged += OnControlSizeChanged;

            base.OnApplyTemplate();
        }

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SizeChanged");

            Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("OnWheelChanged");

            // get keystates
            bool shift = (e.KeyModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;
            bool ctrl = (e.KeyModifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;

            if (ctrl)
            {
                // handle rotation
                if (shift)
                {
                    // store earlier manipulations
                    PointerPoint pointerCenter = e.GetCurrentPoint(_border);
                    Point center = StoreTransformationsAndGetCenter(pointerCenter.Position);
                    //previousTransform.Matrix = transformGroup.Value;
                    //System.Diagnostics.Debug.WriteLine($"Offset ({previousTransform.Matrix.OffsetX},{previousTransform.Matrix.OffsetY})");

                    // init
                    var delta = pointerCenter.Properties.MouseWheelDelta;

                    // init center
                    
                    deltaTransform.CenterX = center.X;
                    deltaTransform.CenterY = center.Y;

                    //var delta = PointerPoint.GetCurrentPoint(e.Pointer.PointerId).Properties.MouseWheelDelta;
                    //// With my mouse, delta is a multiple of 30.
                    //deltaTransform.CenterX = PointerPoint.GetCurrentPoint(e.Pointer.PointerId).Position.X;
                    //deltaTransform.CenterY = PointerPoint.GetCurrentPoint(e.Pointer.PointerId).Position.Y;

                    // rotation
                    if (delta > 0)
                    {
                        deltaTransform.Rotation = -15.0;
                        System.Diagnostics.Debug.WriteLine($"delta > 0 -> {deltaTransform.Rotation}");
                    }
                    else if (delta < 0)
                    {
                        deltaTransform.Rotation = 15.0;
                        System.Diagnostics.Debug.WriteLine($"dekta < 0 -> {deltaTransform.Rotation}");
                    }
                }
                // handle scale
                else
                {
                    // store existing transformation and to prepare new
                    PointerPoint pointerPoint = e.GetCurrentPoint(_border);
                    Point center = StoreTransformationsAndGetCenter(pointerPoint.Position);

                    // reset
                    //ResetTransform<CompositeTransform>();
                    transformGroup.Children.RemoveAt(1);
                    deltaTransform = new CompositeTransform();
                    transformGroup.Children.Add(deltaTransform);

                    // get mouse wheel (touchpad) info
                    double deltaScroll = pointerPoint.Properties.MouseWheelDelta * -1;
                    deltaScroll = (deltaScroll > 0) ? 0.9 : 1.1;

                    // handle zoom/scale
                    deltaTransform.CenterX = center.X;
                    deltaTransform.CenterY = center.Y;
                    deltaTransform.ScaleX = deltaTransform.ScaleY = deltaScroll;

                    TressholdScale();
                }
            }
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Double tapped");

            // store existing transformation and to prepare new
            Point posOnElement = e.GetPosition(_border);
            Point center = StoreTransformationsAndGetCenter(posOnElement);
            //e.GetPosition(Window.Current.CoreWindow.Bounds);

            // reset 
            double existingScale = deltaTransform.ScaleX;
            transformGroup.Children.RemoveAt(1);
            deltaTransform = new CompositeTransform();
            transformGroup.Children.Add(deltaTransform);

            // handle scale
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            if (existingScale < 1.1)
            {
                // zoom in
                deltaTransform.ScaleX = deltaTransform.ScaleY = 3.0;
            }
            else
            {
                // reset zoom
                deltaTransform.ScaleX = deltaTransform.ScaleY = (1 / previousTransform.Matrix.GetScale());
            }
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Manipulation completed");
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Manipulation delta");

            // store existing transformation and to prepare new
            Point center = StoreTransformationsAndGetCenter(e.Position);

            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // rotation
            deltaTransform.Rotation = e.Delta.Rotation;

            // scale
            deltaTransform.ScaleX = e.Delta.Scale;
            deltaTransform.ScaleY = e.Delta.Scale;
            TressholdScale();

            // pan
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
            //ConstrainPan();
        }

        #endregion Methods

        /// <summary>
        /// Store existing transformation in matrix 
        /// and return current center position in respect to this
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Point StoreTransformationsAndGetCenter(Point pos)
        {
            System.Diagnostics.Debug.WriteLine($"Pos: {pos.ToVector2()}");

            // store transformation into matrix
            previousTransform.Matrix = transformGroup.Value;

            // return center in respect to previous transformations
            return previousTransform.TransformPoint(pos);
        }

        public void InitializeAndReset()
        {
            _border.RenderTransform = null;

            transformGroup = new TransformGroup();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };
            deltaTransform = new CompositeTransform();

            transformGroup.Children.Add(previousTransform);
            transformGroup.Children.Add(deltaTransform);

            _border.RenderTransform = transformGroup;
        }
        private void InitializeManipulation()
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

        private void TressholdScale()
        {
            if (transformGroup.Value.GetScale() > MaxZoomFactor || transformGroup.Value.GetScale() < MinZoomFactor)
            {
                deltaTransform.ScaleX = deltaTransform.ScaleY = 1.0;
            }
        }

        private void ResetTransform<T>()
        {
            if (transformGroup != null)
            {
                transformGroup.Children.ToList().RemoveAll(x => x.GetType() is T);
                Transform transform = default(T) as Transform;
                transformGroup.Children.Add(transform);
            }
        }
    }
}
