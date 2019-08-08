#if TOOLS

using Godot;
using Wayfarer.ModuleSystem;

namespace Wayfarer.UI
{
    [Tool]
    public class UICorePlugin : WayfarerModule
    {
        public override void _EnterTreeSafe()
        {
            AddCustomTypes();
        }

        public override void _ExitTreeSafe()
        {
            RemoveCustomTypes();
        }

        private void AddCustomTypes()
        {
            Texture tempIcon = GD.Load<Texture>("res://Addons/Wayfarer.Core/Assets/Icons/manager.png");
            Texture defaultIcon = GD.Load<Texture>("res://icon.png");
            
            Script organizableContainer = GD.Load<Script>("res://Addons/Wayfarer.UI/Controls/OrganizableContainer/OrganizableContainer.cs");
            if (organizableContainer != null)
            {
                AddCustomType("OrganizableContainer", "Control", organizableContainer, tempIcon ?? defaultIcon);
            }
            
            Script organizableItem = GD.Load<Script>("res://Addons/Wayfarer.UI/Controls/OrganizableItem/OrganizableItem.cs");
            if (organizableItem != null)
            {
                AddCustomType("OrganizableItem", "Control", organizableItem, tempIcon ?? defaultIcon);
            }
        }

        private void RemoveCustomTypes()
        {
            RemoveCustomType("OrganizableContainer");
            RemoveCustomType("OrganizableItem");
        }
        
    }
}

#endif