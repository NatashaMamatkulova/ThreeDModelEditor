using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ThreeDModelEditor
{
    public class SceneObjectInfo
    {
        public string Name { get; set; }
        public string Layer { get; set; }
        public ModelVisual3D Visual { get; set; }
        public object OriginalContent { get; set; }
        public bool IsVisible { get; set; } = true;
        public Transform3D Transform { get; set; }

    }
}
