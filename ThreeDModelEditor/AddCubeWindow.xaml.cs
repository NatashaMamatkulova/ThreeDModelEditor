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
using System.Windows.Shapes;

namespace ThreeDModelEditor
{
    /// <summary>
    /// Логика взаимодействия для AddCubeWindow.xaml
    /// </summary>
    public partial class AddCubeWindow : Window
    {
        public double CubeWidth { get; private set; }
        public double CubeHeight { get; private set; }
        public double CubeDepth { get; private set; }

        public AddCubeWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(WidthBox.Text, out double width) &&
                double.TryParse(HeightBox.Text, out double height) &&
                double.TryParse(DepthBox.Text, out double depth))
            {
                CubeWidth = width;
                CubeHeight = height;
                CubeDepth = depth;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректные числовые значения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
