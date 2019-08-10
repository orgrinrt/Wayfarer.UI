using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Godot;
using Godot.Collections;
using Wayfarer.Core.Systems.Managers;
using Wayfarer.Editor;
using Wayfarer.Utils.Debug;
using Array = Godot.Collections.Array;

namespace Wayfarer.UI.Controls
{
    #if TOOLS
    [Tool]
    #endif
    public class OrganizableContainer : WayfarerControl
    {
        private OrganizingMode _organizingMode = OrganizingMode.Horizontal;
        private SortDirection _sortDirection = SortDirection.Right;
        private SwitchThreshold _switchThreshold = SwitchThreshold.Middle;
        private bool _regularSizeChildren = false;
        private bool _lowPerformance = false;
        private float _sortAnimDuration = 0.4f;
        private float _separation = 5f;
        private float _sortPrecision = 0.5f;
        
        private float _regularSize = float.MaxValue;
        private bool _isMouseOver = false;
        private OrganizableItem _draggedChild;
        private bool _hasNonOrganizableChildren = false;
        private float _axisAnchor = float.MaxValue;
        private bool _changed = false;
        
        

        public OrganizingMode OrganizingMode => _organizingMode;
        public SortDirection SortDirection => _sortDirection;
        public SwitchThreshold SwitchThreshold => _switchThreshold;
        public bool RegularSizeChildren => _regularSizeChildren;
        public bool LowPerformance => _lowPerformance;
        public float SortAnimDuration => _sortAnimDuration;
        public float Separation => _separation;
        public float SortPrecision => _sortPrecision;
        
        public float RegularSize => _regularSize;
        public bool IsMouseOver => _isMouseOver;
        public OrganizableItem DraggedChild => _draggedChild;
        public bool HasNonOrganizableChildren => _hasNonOrganizableChildren;
        public float AxisAnchor => _axisAnchor;
        public bool Changed => _changed;
        
        public int CurrHoveredIndex => GetCurrHoveredIndex();
        
        public override void _ReadySafe()
        {
            Connect("mouse_entered", this, nameof(OnMouseEntered));
            Connect("mouse_exited", this, nameof(OnMouseExited));
            
            try
            {
                Connections.Add(MouseManager, nameof(MouseManager.StoppedDragging), this, nameof(OnGlobalDragStopped));
                Connections.Add(MouseManager, nameof(MouseManager.StartedDragging), this, nameof(OnGlobalDragStarted));
            }
            catch (Exception e)
            {
                Connections.Add(GetMouseManager, nameof(MouseManager.StoppedDragging), this, nameof(OnGlobalDragStopped));
                Connections.Add(GetMouseManager, nameof(MouseManager.StartedDragging), this, nameof(OnGlobalDragStarted));
            }

            if (GetChildCount() > 0 && Math.Abs(_axisAnchor - float.MaxValue) < SortPrecision)
            {
                Control child = GetChild<Control>(0);
                
                if (OrganizingMode == OrganizingMode.Horizontal)
                {
                    _axisAnchor = child.RectPosition.y;
                    _regularSize = child.RectSize.x;
                }
                else if (OrganizingMode == OrganizingMode.Vertical)
                {
                    _axisAnchor = child.RectPosition.x;
                    _regularSize = child.RectSize.y;
                }
            }
        }

        public override string _GetConfigurationWarning()
        {
            if (HasNonOrganizableChildren)
            {
                string errorMsg = "This container can only have children of type \"OrganizableItem\" " +
                                  "Please remove any other kind of children, or alternatively, derive the children from it.";
                return errorMsg;
            }
            
            return "";
        }

        public override Array _GetPropertyList()
        {
            Array list = new Array();

            Dictionary header = new Dictionary
            {
                {"name", "OrganizableContainer"},
                {"type", Variant.Type.Nil},
                {"usage", PropertyUsageFlags.Category}
            };
            list.Add(header);
            
            Dictionary orgMode = new Dictionary
            {
                {"name", "layout/organizing_mode"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "Horizontal, Vertical"},
            };
            list.Add(orgMode);
            
            Dictionary sortDir = new Dictionary
            {
                {"name", "behaviour/sort_direction"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "Right, Left"},
            };
            list.Add(sortDir);
            
            Dictionary switchThreshold = new Dictionary
            {
                {"name", "behaviour/switch_threshold"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "End, Middle, Start"},
            };
            list.Add(switchThreshold);
            
            Dictionary separation = new Dictionary
            {
                {"name", "layout/separation"},
                {"type", Variant.Type.Real},
            };
            list.Add(separation);
            
            Dictionary precision = new Dictionary
            {
                {"name", "behaviour/sort_precision"},
                {"type", Variant.Type.Real},
            };
            list.Add(precision);
            
            Dictionary sortAnimDur = new Dictionary
            {
                {"name", "behaviour/sort_animation_duration"},
                {"type", Variant.Type.Real},
            };
            list.Add(sortAnimDur);
            
            Dictionary regChildren = new Dictionary
            {
                {"name", "optimizations/regular_sized_children"},
                {"type", Variant.Type.Bool},
            };
            list.Add(regChildren);
            
            Dictionary lowPerf = new Dictionary
            {
                {"name", "optimizations/low_performance_mode"},
                {"type", Variant.Type.Bool},
            };
            list.Add(lowPerf);

            return list;
        }

        public override object _Get(string property)
        {
            switch (property)
            {
                case "layout/organizing_mode":
                    return OrganizingMode;
                case "behaviour/sort_direction":
                    return SortDirection;
                case "behaviour/switch_threshold":
                    return SwitchThreshold;
                case "layout/separation":
                    return Separation;
                case "behaviour/sort_precision":
                    return SortPrecision;
                case "behaviour/sort_animation_duration":
                    return SortAnimDuration;
                case "optimizations/regular_sized_children":
                    return RegularSizeChildren;
                case "optimizations/low_performance_mode":
                    return LowPerformance;
            }
            
            return base._Get(property);
        }

        public override bool _Set(string property, object value)
        {
            switch (property)
            {
                case "layout/organizing_mode":
                    _organizingMode = (OrganizingMode) value;
                    break;
                case "behaviour/sort_direction":
                    _sortDirection = (SortDirection) value;
                    break;
                case "behaviour/switch_threshold":
                    _switchThreshold = (SwitchThreshold) value;
                    break;
                case "layout/separation":
                    _separation = (float) value;
                    break;
                case "behaviour/sort_precision":
                    _sortPrecision = (float) value;
                    break;
                case "behaviour/sort_animation_duration":
                    _sortAnimDuration = (float) value;
                    break;
                case "optimizations/regular_sized_children":
                    _regularSizeChildren = (bool) value;
                    break;
                case "optimizations/low_performance_mode":
                    _lowPerformance = (bool) value;
                    break;
            }
            
            return base._Set(property, value);
        }

        public override void _ProcessSafe(float delta)
        {
            #if TOOLS
            if (HasNonOrganizableChildren)
            {
                _hasNonOrganizableChildren = false;
            }
            #endif
            
            if (DraggedChild != null)
            {
                int hover = GetCurrHoveredIndex();
                if (DraggedChild.GetIndex() != hover && hover > -1 && hover < GetChildCount())
                {
                    MoveChild(DraggedChild, hover);
                    _changed = true;
                }
            }
            
            if (Changed)
            {
                if (IsSortDone())
                {
                    _changed = false;
                    return;
                }
                SortAll();
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton)
            {
                _changed = true;
            }
        }

        public int GetCurrHoveredIndex()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (!IsMouseOver)
                {
                    return -1;
                }
                /*
                if (RegularSizeChildren)
                {
                    // wonder if there's optimizations to do here
                }*/
                else
                {
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item)
                        {
                            if (item.IsDragged)
                            {
                                _draggedChild = item;
                            }
                            
                            if (SortDirection == SortDirection.Right)
                            {
                                float thresholdX = GetItemXPosByIndex(item.GetIndex());

                                switch (SwitchThreshold)
                                {
                                    case SwitchThreshold.End:
                                        thresholdX += (item.RectSize.x * 1f);
                                        break;
                                    case SwitchThreshold.Middle:
                                        thresholdX += (item.RectSize.x * 1f) + (Separation / 2);
                                        break;
                                    case SwitchThreshold.Start:
                                        thresholdX += (item.RectSize.x * 1f) + Separation;
                                        break;
                                }
                                
                                if (GetLocalMousePosition().x > thresholdX)
                                {
                                    continue;
                                }
                                
                                return item.GetIndex();
                            }
                            else if (SortDirection == SortDirection.Left)
                            {
                                float thresholdX = GetItemXPosByIndex(item.GetIndex());

                                switch (SwitchThreshold)
                                {
                                    case SwitchThreshold.End:
                                        break;
                                    case SwitchThreshold.Middle:
                                        thresholdX -= (Separation / 2);
                                        break;
                                    case SwitchThreshold.Start:
                                        thresholdX -= Separation;
                                        break;
                                }
                                
                                if (GetLocalMousePosition().x < thresholdX)
                                {
                                    continue;
                                }
                                
                                return item.GetIndex();
                            }
                        }
                        else
                        {
                            _hasNonOrganizableChildren = true;
                        }
                    }
                }
            }
            
            return GetChildCount() - 1;
        }

        private void SortAll()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && !item.IsDragged)
                    {
                        SortX(item);
                    }
                }
            }
        }

        private void SortY(OrganizableItem item)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                
            }
        }

        private void SortX(OrganizableItem item)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (item.Tween == null)
                {
                    item.CreateTween();
                }
                
                if (LowPerformance && item.Tween.IsActive())
                {
                    return;
                }
                        
                if (SortDirection == SortDirection.Right)
                {
                    if (Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) > SortPrecision)
                    {
                        Vector2 newPos = new Vector2(GetItemXPosByIndex(item.GetIndex()), AxisAnchor);
                                
                        item.Tween.Stop(item, "rect_position");
                                
                        item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                            SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                
                        item.Tween.Start();
                    }
                }
                else if (SortDirection == SortDirection.Left)
                {
                    if (Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) > SortPrecision)
                    {
                        Vector2 newPos = new Vector2(GetItemXPosByIndex(item.GetIndex()), AxisAnchor);
                                
                        item.Tween.Stop(item, "rect_position");
                                
                        item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                            SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                
                        item.Tween.Start();
                    }
                }
            }
        }

        private float GetItemXPosByIndex(int idx)
        {
            if (RegularSizeChildren)
            {
                if (SortDirection == SortDirection.Right)
                {
                    return idx * (RegularSize + Separation);
                }
                else if (SortDirection == SortDirection.Left)
                {
                    //float maxX = GetChildCount() * (RegularSize + Separation);
                    float maxX = RectSize.x - RegularSize;
                    
                    return maxX - (idx * (RegularSize + Separation));
                }
            }
            else
            {
                float currEndX = SortDirection == SortDirection.Right ? 0f : (RectSize.x - GetChild<Control>(GetChildCount()-1).RectSize.x);

                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && item.GetIndex() <= idx - 1)
                    {
                        if (SortDirection == SortDirection.Right)
                        {
                            currEndX += (item.RectSize.x + Separation);
                        }
                        else if (SortDirection == SortDirection.Left)
                        {
                            currEndX -= (item.RectSize.x + Separation);
                        }
                    }
                    else break;
                }

                return currEndX;
            }

            return 0f;
        }

        public bool IsSortDone()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (RegularSizeChildren)
                {
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item && !item.IsDragged)
                        {
                            if (Math.Abs(item.RectPosition.y - AxisAnchor) > SortPrecision)
                            {
                                return false;
                            }

                            if (Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) > SortPrecision)
                            {
                                return false;
                            }
                        }
                        else if (!(node is OrganizableItem))
                        {
                            _hasNonOrganizableChildren = true;
                        }
                        else
                        {
                            _draggedChild = node as OrganizableItem;
                            return false;
                        }
                    }

                    return true;
                }
                else
                {
                    float currEndX = SortDirection == SortDirection.Right ? 0f : RectSize.x - GetChild<Control>(GetChildCount()-1).RectSize.x;

                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item && !item.IsDragged)
                        {
                            int nextIdx = item.GetIndex() + 1;

                            if (nextIdx == GetChildCount())
                            {
                                return true;
                            }

                            if (Math.Abs(item.RectPosition.y - AxisAnchor) > SortPrecision)
                            {
                                return false;
                            }

                            if (SortDirection == SortDirection.Right)
                            {
                                currEndX += (item.RectSize.x + Separation);

                                Node nextNode = GetChild(nextIdx);
                                if (nextNode is OrganizableItem nextItem)
                                {
                                    if (Math.Abs(nextItem.RectPosition.x - currEndX) > SortPrecision)
                                    {
                                        return false;
                                    }
                                }
                            }
                            else if (SortDirection == SortDirection.Left)
                            {
                                currEndX -= (item.RectSize.x + Separation); 

                                Node nextNode = GetChild(nextIdx);
                                if (nextNode is OrganizableItem nextItem)
                                {
                                    if (Math.Abs(nextItem.RectPosition.x - currEndX) > SortPrecision)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        else if (!(node is OrganizableItem))
                        {
                            _hasNonOrganizableChildren = true;
                        }
                        else
                        {
                            _draggedChild = node as OrganizableItem;
                            return false;
                        }
                    }
                }
            }
            
            return false;
        }

        private void OnMouseEntered()
        {
            _isMouseOver = true;
        }

        private void OnMouseExited()
        {
            _isMouseOver = false;
        }

        private void OnGlobalDragStopped(Node node)
        {
            _draggedChild = null;
            
            if (node is OrganizableItem item)
            {
                foreach (Node child in GetChildren())
                {
                    if (item == child)
                    {
                        item.SetIsDragged(false);
                        MoveChild(item, CurrHoveredIndex);
                        return;
                    }
                }
            }
        }

        private void OnGlobalDragStarted(Node node)
        {
            if (node is OrganizableItem item)
            {
                foreach (Node child in GetChildren())
                {
                    if (item == child)
                    {
                        item.SetIsDragged(true);
                        _draggedChild = item;
                        return;
                    }
                }
            }
        }

        public void SetRegularSizedChildren(bool value)
        {
            _regularSizeChildren = value;
            
            if (GetChildCount() > 0)
            {
                Control child = GetChild<Control>(0);
                
                if (OrganizingMode == OrganizingMode.Horizontal)
                {
                    _regularSize = child.RectSize.x;
                }
                else if (OrganizingMode == OrganizingMode.Vertical)
                {
                    _regularSize = child.RectSize.y;
                }
            }
        }

        public void SetSortDirection(SortDirection dir)
        {
            _sortDirection = dir;
        }
    }
    
    public enum OrganizingMode
    {
        
        Horizontal,
        Vertical
    }

    public enum SortDirection
    {
        Right,
        Left
    }

    public enum SwitchThreshold
    {
        End,
        Middle,
        Start
    }
}