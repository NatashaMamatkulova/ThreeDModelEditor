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
    /// Логика взаимодействия для AddPyramidWindow.xaml
    /// </summary>
    public partial class AddPyramidWindow : Window
    {
        public double Radius { get; private set; }
        public double PyramidHeight { get; private set; }
        public int Sides { get; private set; }

        public AddPyramidWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(RadiusBox.Text, out double radius) &&
                double.TryParse(HeightBox.Text, out double height) &&
                int.TryParse(SidesBox.Text, out int sides))
            {
                Radius = radius;
                PyramidHeight = height;
                Sides = sides;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректные значения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
