using Godot;
using Wayfarer.Core.Systems.Managers;

namespace Wayfarer.UI.Controls
{
    public interface IDraggableControl
    {
        bool IsDragged { get; }
        MouseManager MouseManager { get;  }
        void StartDrag();
        void EndDrag(Node node);
        MouseManager GetMouseManager();
    }
}