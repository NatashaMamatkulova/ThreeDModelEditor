using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ThreeDModelEditor
{
    enum ActionType
    {
        Add,
        Remove,
        Transform
    }

    class SceneAction
    {
        public ActionType Type { get; set; }
        public ModelVisual3D Target { get; set; }
        public Transform3D OldTransform { get; set; }
        public Transform3D NewTransform { get; set; }
    }
}
