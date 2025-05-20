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
    /// Логика взаимодействия для AddSphereWindow.xaml
    /// </summary>
    public partial class AddSphereWindow : Window
    {
        public double Radius { get; private set; }
        public int Segments { get; private set; }

        public AddSphereWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(RadiusBox.Text, out double radius) &&
                int.TryParse(SegmentsBox.Text, out int segments) &&
                radius > 0 && segments > 4)
            {
                Radius = radius;
                Segments = segments;
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
