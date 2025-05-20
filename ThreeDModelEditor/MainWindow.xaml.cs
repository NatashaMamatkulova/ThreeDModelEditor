using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System.IO;

namespace ThreeDModelEditor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DirectionalLight headLight;
        private ModelVisual3D headLightVisual;
        private List<ModelVisual3D> selectedObjects = new List<ModelVisual3D>();
        private Material defaultMaterial = Materials.LightGray;
        private Material selectedMaterial = MaterialHelper.CreateMaterial(Colors.SkyBlue);
        private Stack<SceneAction> undoStack = new Stack<SceneAction>();
        private Stack<SceneAction> redoStack = new Stack<SceneAction>();
        private const int MaxUndoSteps = 50;

        public MainWindow()
        {
            InitializeComponent();
            CompositionTarget.Rendering += OnRendering;
            this.KeyDown += MainWindow_KeyDown;
            this.Focusable = true;
            this.Focus();
            InitializeScene();
            
        }
        private void OnRendering(object sender, EventArgs e)
        {
            if (viewport.Camera is ProjectionCamera cam && headLight != null)
            {
                Vector3D dir = cam.LookDirection;
                dir.Normalize();
                headLight.Direction = dir;
            }
        }


        private void AddUndoAction(SceneAction action)
        {
            undoStack.Push(action);

            // до 50
            if (undoStack.Count > MaxUndoSteps)
                undoStack = new Stack<SceneAction>(undoStack.Reverse().Take(MaxUndoSteps));

            redoStack.Clear(); // новая операция сбрасывает повтор
        }

        // горячие клавиши
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Delete)
            {
                foreach (var obj in selectedObjects)
                {
                    AddUndoAction(new SceneAction
                    {
                        Type = ActionType.Remove,
                        Target = obj
                    });

                    viewport.Children.Remove(obj);
                }
            }

            // отмена
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (undoStack.Count > 0)
                {
                    var action = undoStack.Pop();
                    switch (action.Type)
                    {
                        case ActionType.Add:
                            viewport.Children.Remove(action.Target);
                            break;
                        case ActionType.Remove:
                            viewport.Children.Add(action.Target);
                            break;
                        case ActionType.Transform:
                            action.Target.Transform = action.OldTransform;
                            break;
                    }
                    redoStack.Push(action);
                }
                return;
            }

            // повтор
            if (e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (redoStack.Count > 0)
                {
                    var action = redoStack.Pop();
                    switch (action.Type)
                    {
                        case ActionType.Add:
                            viewport.Children.Add(action.Target);
                            break;
                        case ActionType.Remove:
                            viewport.Children.Remove(action.Target);
                            break;
                        case ActionType.Transform:
                            action.Target.Transform = action.NewTransform;
                            break;
                    }
                    undoStack.Push(action);
                }
                return;
            }


            if (selectedObjects.Count > 0)
            {
                AxisAngleRotation3D rotation = null;
                const double angle = 10; // угол в градусах за одно нажатие

                switch (e.Key)
                {
                    case Key.NumPad7:
                        rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -angle); break;
                    case Key.NumPad1:
                        rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), angle); break;

                    case Key.NumPad9:
                        rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -angle); break;
                    case Key.NumPad3:
                        rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), angle); break;

                    case Key.Add: // NumPad +
                        rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), -angle); break;
                    case Key.Subtract: // NumPad -
                        rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), angle); break;
                }

                if (rotation != null)
                {
                    foreach (var visual in selectedObjects)
                    {
                        var oldTransform = visual.Transform?.Clone();

                        var rotate = new RotateTransform3D(rotation, new Point3D(0, 0, 0));

                        if (visual.Transform is Transform3DGroup group)
                        {
                            group.Children.Add(rotate);
                        }
                        else if (visual.Transform != null)
                        {
                            var newGroup = new Transform3DGroup();
                            newGroup.Children.Add(visual.Transform);
                            newGroup.Children.Add(rotate);
                            visual.Transform = newGroup;
                        }
                        else
                        {
                            visual.Transform = rotate;
                        }

                        var newTransform = visual.Transform?.Clone();

                        AddUndoAction(new SceneAction
                        {
                            Type = ActionType.Transform,
                            Target = visual,
                            OldTransform = oldTransform,
                            NewTransform = newTransform
                        });
                    }

                    return;
                }
            }
            const double step = 0.5;

            if (selectedObjects.Count == 0)
                return;

            Vector3D offset = new Vector3D();

            switch (e.Key)
            {
                case Key.NumPad4: offset.X -= step; break;
                case Key.NumPad6: offset.X += step; break;
                case Key.NumPad8: offset.Z -= step; break;
                case Key.NumPad2: offset.Z += step; break;
                case Key.NumPad5: offset.Y += step; break;
                case Key.NumPad0: offset.Y -= step; break;
                default: return;
            }



            foreach (var visual in selectedObjects)
            {
                var oldTransform = visual.Transform?.Clone(); // сохранить старое

                // Трансформация: добавим в Transform3DGroup
                var offsetTransform = new TranslateTransform3D(offset);

                if (visual.Transform is Transform3DGroup group)
                {
                    group.Children.Add(offsetTransform);
                }
                else
                {
                    var newGroup = new Transform3DGroup();
                    if (visual.Transform != null)
                        newGroup.Children.Add(visual.Transform);
                    newGroup.Children.Add(offsetTransform);
                    visual.Transform = newGroup;
                }

                // Сохраняем в undo стек
                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Transform,
                    Target = visual,
                    OldTransform = oldTransform?.Clone(),
                    NewTransform = visual.Transform?.Clone()
                });
            }
        }

        // оси в центре
        private void AddAxes()
        {
            var axes = new LinesVisual3D
            {
                Thickness = 2
            };

            axes.Points.Add(new Point3D(0, 0, 0));
            axes.Points.Add(new Point3D(5, 0, 0)); 
            axes.Color = Colors.Red;
            viewport.Children.Add(axes);

            var axesY = new LinesVisual3D
            {
                Thickness = 2,
                Color = Colors.Green,
                Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 5, 0) }
            };
            viewport.Children.Add(axesY);

            var axesZ = new LinesVisual3D
            {
                Thickness = 2,
                Color = Colors.Blue,
                Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 0, 5) }
            };
            viewport.Children.Add(axesZ);
        }

        // выделить объект
        private void viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (viewport.Viewport == null)
            {
                MessageBox.Show("Viewport ещё не инициализирован");
                return;
            }

            Point pos = e.GetPosition(viewport);
            var hits = Viewport3DHelper.FindHits(viewport.Viewport, pos);

            // клик по фону: снять выделение
            if (hits.Count == 0)
            {
                foreach (var obj in selectedObjects)
                {
                    if (obj.Content is GeometryModel3D g)
                        g.Material = defaultMaterial;

                    if (obj.Content is Model3DGroup group)
                    {
                        foreach (var child in group.Children)
                        {
                            if (child is GeometryModel3D cg)
                                cg.Material = defaultMaterial;
                        }
                    }
                }

                selectedObjects.Clear();
                return;
            }

            var hit = hits[0];
            var visual = hit.Visual as ModelVisual3D;
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (!ctrl)
            {
                // снять предыдущее выделение
                foreach (var obj in selectedObjects)
                {
                    if (obj.Content is GeometryModel3D g)
                        g.Material = defaultMaterial;

                    if (obj.Content is Model3DGroup group)
                    {
                        foreach (var child in group.Children)
                        {
                            if (child is GeometryModel3D cg)
                                cg.Material = defaultMaterial;
                        }
                    }
                }

                selectedObjects.Clear();
            }

            if (visual?.Content is GeometryModel3D model)
            {
                
                model.Material = selectedMaterial;
                model.BackMaterial = selectedMaterial;
                selectedObjects.Add(visual);
            }
            else if (visual?.Content is Model3DGroup group)
            {
                foreach (var child in group.Children)
                {
                    if (child is GeometryModel3D childModel)
                        childModel.Material = selectedMaterial;

                }

                selectedObjects.Add(visual);
            }
            
            if (selectedObjects.Count == 1 && selectedObjects[0].Content is GeometryModel3D geom)
            {
                var bounds = GetTransformedBounds(selectedObjects[0]);
                SizeXBox.Text = bounds.SizeX.ToString("F2");
                SizeYBox.Text = bounds.SizeY.ToString("F2");
                SizeZBox.Text = bounds.SizeZ.ToString("F2");
            }
        }

        // новая сцена: начальная сцена при запуске, удаление фигуры, оставить необходимое освещение
        private void InitializeScene()
        {
            viewport.Children.Clear();

            viewport.Background = new LinearGradientBrush(
                Color.FromRgb(238, 238, 238),
                Color.FromRgb(150, 190, 190),
                new Point(0, 0),
                new Point(0, 1));

            var camera = new PerspectiveCamera
            {
                Position = new Point3D(20, 20, 20),
                LookDirection = new Vector3D(-10, -10, -10),
                UpDirection = new Vector3D(0, 1, 0)

            };
            viewport.Camera = camera;

            // создаём свет, который будет обновляться при вращении
            headLight = new DirectionalLight(Color.FromRgb(100, 100, 100), camera.LookDirection);
            headLightVisual = new ModelVisual3D { Content = headLight };

            viewport.Children.Add(headLightVisual);

            var ambient = new AmbientLight(Colors.Gray);
            viewport.Children.Add(new ModelVisual3D { Content = ambient });
            AddAxes();


        }

        // открыть новую сцену
        private void NewScene_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите очистить сцену? Все несохранённые изменения будут потеряны.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                InitializeScene();
            }
            // Иначе ничего не делаем
        }

        // открыть модель
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "3D модели (*.obj;*.stl)|*.obj;*.stl|OBJ файлы (*.obj)|*.obj|STL файлы (*.stl)|*.stl"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string extension = System.IO.Path.GetExtension(filePath).ToLower();

                Model3D model = null;

                try
                {
                    if (extension == ".obj")
                    {
                        var importer = new ModelImporter();
                        model = importer.Load(filePath);
                    }
                    else if (extension == ".stl")
                    {
                        var reader = new StLReader();
                        model = reader.Read(filePath);
                    }

                    if (model != null)
                    {
                        var modelVisual = new ModelVisual3D { Content = model };
                        viewport.Children.Add(modelVisual);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось загрузить модель.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке файла:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // сохранить модель
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "OBJ Files (*.obj)|*.obj|STL Files (*.stl)|*.stl",
                DefaultExt = "obj"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string ext = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();

                switch (ext)
                {
                    case ".obj":
                        var objExporter = new HelixToolkit.Wpf.ObjExporter();
                        using (var stream = File.Create(saveFileDialog.FileName))
                            objExporter.Export(viewport.Viewport, stream);
                        break;

                    case ".stl":
                        var stlExporter = new HelixToolkit.Wpf.StlExporter();
                        using (var stream = File.Create(saveFileDialog.FileName))
                            stlExporter.Export(viewport.Viewport, stream);
                        break;

                    default:
                        MessageBox.Show("Неподдерживаемое расширение файла.");
                        break;
                }
            }
        }

        // панель
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = RightPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // удалить
        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var obj in selectedObjects.ToList())
            {
                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Remove,
                    Target = obj
                });

                viewport.Children.Remove(obj);
                selectedObjects.Remove(obj);
            }
        }

        

        // добавить куб
        private void AddCube_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCubeWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                // Параметры
                double width = dialog.CubeWidth;
                double height = dialog.CubeHeight;
                double depth = dialog.CubeDepth;

                // Создание куба
                var meshBuilder = new HelixToolkit.Wpf.MeshBuilder();
                meshBuilder.AddBox(center: new Point3D(0, 0, 0), xlength: width, ylength: height, zlength: depth);
                var mesh = meshBuilder.ToMesh(true);

                var material = Materials.LightGray;
                var model = new GeometryModel3D(mesh, material);

                var visual = new ModelVisual3D { Content = model };
                viewport.Children.Add(visual);
                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Add,
                    Target = visual
                });
            }
        }

        // добавить сферу
        private void AddSphere_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSphereWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                double radius = dialog.Radius;
                int segments = dialog.Segments;

                var meshBuilder = new MeshBuilder();
                meshBuilder.AddSphere(center: new Point3D(0, 0, 0), radius: radius, thetaDiv: segments, phiDiv: segments);

                var mesh = meshBuilder.ToMesh(true);
                var material = Materials.LightGray;
                var model = new GeometryModel3D(mesh, material);

                var visual = new ModelVisual3D { Content = model };
                viewport.Children.Add(visual);
                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Add,
                    Target = visual
                });
            }
        }

        //добавить пирамиду/конус
        private void AddPyramid_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPyramidWindow { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                double radius = dialog.Radius;
                double height = dialog.PyramidHeight;
                int sides = dialog.Sides;

                var meshBuilder = new MeshBuilder();

                if (sides < 3)
                {
                    // Конус
                    meshBuilder.AddCone(
                        new Point3D(0, 0, 0),
                        new Vector3D(0, height, 0),
                        radius,
                        0,
                        height,
                        true,
                        false,
                        40);
                }
                else
                {
                    // Пирамида (многоугольник в основании)
                    Point3D apex = new Point3D(0, height, 0);
                    Point3D center = new Point3D(0, 0, 0);

                    double angleStep = 2 * Math.PI / sides;
                    List<Point3D> basePoints = new List<Point3D>();

                    for (int i = 0; i < sides; i++)
                    {
                        double angle = i * angleStep;
                        double x = radius * Math.Cos(angle);
                        double z = radius * Math.Sin(angle);
                        basePoints.Add(new Point3D(x, 0, z));
                    }

                    // Добавим треугольные боковые грани
                    for (int i = 0; i < sides; i++)
                    {
                        Point3D p1 = basePoints[i];
                        Point3D p2 = basePoints[(i + 1) % sides];
                        meshBuilder.AddTriangle(p2, p1, apex);
                    }

                    // Добавим основание (замкнутый многоугольник)
                    for (int i = 1; i < sides - 1; i++)
                    {
                        meshBuilder.AddTriangle(basePoints[0], basePoints[i], basePoints[i + 1]);
                    }
                }

                var mesh = meshBuilder.ToMesh(true);
                var material = Materials.LightGray;
                var model = new GeometryModel3D(mesh, material);
                var visual = new ModelVisual3D { Content = model };

                viewport.Children.Add(visual);
                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Add,
                    Target = visual
                });
            }
        }

        //добавить цилиндер
        private void AddCylinder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCylinderWindow { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                double radius = dialog.Radius;
                double height = dialog.CylinderHeight;
                int sides = dialog.Sides;

                
                if (sides < 3)
                {
                    sides = 64;
                }

                var meshBuilder = new MeshBuilder();
                meshBuilder.AddCylinder(
                    new Point3D(0, 0, 0),              // основание
                    new Point3D(0, height, 0),         // вершина
                    radius,
                    sides,
                    true,                              // с нижним основанием
                    true);                             // с верхним основанием

                var mesh = meshBuilder.ToMesh(true);
                var material = Materials.LightGray;
                var model = new GeometryModel3D(mesh, material);
                var visual = new ModelVisual3D { Content = model };

                viewport.Children.Add(visual);
                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Add,
                    Target = visual
                });
            }
        }


        //изменение размера
        private void ApplyScale_Click(object sender, RoutedEventArgs e)
        {
            if (selectedObjects.Count != 1)
            {
                MessageBox.Show("Выберите один объект для масштабирования.");
                return;
            }

            if (double.TryParse(SizeXBox.Text, out double targetX) &&
                double.TryParse(SizeYBox.Text, out double targetY) &&
                double.TryParse(SizeZBox.Text, out double targetZ))
            {
                var target = selectedObjects[0];
                var oldTransform = target.Transform?.Clone();

                var bounds = GetTransformedBounds(target);

                if (bounds.SizeX <= 0 || bounds.SizeY <= 0 || bounds.SizeZ <= 0)
                {
                    MessageBox.Show("Некорректный объект для масштабирования.");
                    return;
                }

                var scaleX = targetX / bounds.SizeX;
                var scaleY = targetY / bounds.SizeY;
                var scaleZ = targetZ / bounds.SizeZ;

                var center = new Point3D(bounds.X + bounds.SizeX / 2, bounds.Y + bounds.SizeY / 2, bounds.Z + bounds.SizeZ / 2);
                var scale = new ScaleTransform3D(scaleX, scaleY, scaleZ, center.X, center.Y, center.Z);

                if (target.Transform is Transform3DGroup group)
                {
                    group.Children.Add(scale);
                }
                else
                {
                    var newGroup = new Transform3DGroup();
                    if (target.Transform != null)
                        newGroup.Children.Add(target.Transform);
                    newGroup.Children.Add(scale);
                    target.Transform = newGroup;
                }

                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Transform,
                    Target = target,
                    OldTransform = oldTransform,
                    NewTransform = target.Transform?.Clone()
                });

                // обновим текстбоксы с актуальными размерами
                var newBounds = GetTransformedBounds(target);
                SizeXBox.Text = newBounds.SizeX.ToString("F2");
                SizeYBox.Text = newBounds.SizeY.ToString("F2");
                SizeZBox.Text = newBounds.SizeZ.ToString("F2");
            }
            else
            {
                MessageBox.Show("Введите корректные значения.");
            }
        }

        //запомнить
        private Rect3D GetTransformedBounds(ModelVisual3D visual)
        {
            if (visual.Content is GeometryModel3D geom)
            {
                var bounds = geom.Geometry.Bounds;
                var transform = visual.Transform ?? Transform3D.Identity;
                return transform.TransformBounds(bounds);
            }
            return new Rect3D();
        }

        //передвинуть
        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(MoveXBox.Text, out double dx)) dx = 0;
            if (!double.TryParse(MoveYBox.Text, out double dy)) dy = 0;
            if (!double.TryParse(MoveZBox.Text, out double dz)) dz = 0;

            Vector3D offset = new Vector3D(dx, dy, dz);

            foreach (var visual in selectedObjects)
            {
                var oldTransform = visual.Transform?.Clone();

                var translate = new TranslateTransform3D(offset);
                Transform3DGroup group = new Transform3DGroup();

                if (visual.Transform != null)
                    group.Children.Add(visual.Transform);
                group.Children.Add(translate);

                visual.Transform = group;

                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Transform,
                    Target = visual,
                    OldTransform = oldTransform,
                    NewTransform = visual.Transform?.Clone()
                });
            }
        }

        //повращать
        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(RotateXBox.Text, out double angleX)) angleX = 0;
            if (!double.TryParse(RotateYBox.Text, out double angleY)) angleY = 0;
            if (!double.TryParse(RotateZBox.Text, out double angleZ)) angleZ = 0;

            foreach (var visual in selectedObjects)
            {
                var oldTransform = visual.Transform?.Clone();

                var group = new Transform3DGroup();

                if (visual.Transform != null)
                    group.Children.Add(visual.Transform);

                if (angleX != 0)
                    group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), angleX)));
                if (angleY != 0)
                    group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), angleY)));
                if (angleZ != 0)
                    group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), angleZ)));

                visual.Transform = group;

                AddUndoAction(new SceneAction
                {
                    Type = ActionType.Transform,
                    Target = visual,
                    OldTransform = oldTransform,
                    NewTransform = visual.Transform?.Clone()
                });
            }
        }

    }
}
