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
        private SortDirection _sortDirection = SortDirection.Forward;
        private SwitchThreshold _switchThreshold = SwitchThreshold.Middle;
        private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
        private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
        private Array _acceptedGroups;
        private bool _allowDroppingFromOtherContainers = true;
        private bool _regularSizeChildren = false;
        private bool _lowPerformance = false;
        private bool _noShifting = false;
        private float _sortAnimDuration = 0.4f;
        private float _separation = 5f;
        private float _sortPrecision = 0.5f;
        private bool _separationOnEnds = false;
        private bool _wrap = true;
        
        private Vector2 _regularSize = Vector2.Zero;
        private bool _isMouseOver = false;
        private OrganizableItem _draggedChild;
        private bool _changed = false;
        private Tween _tween;
        
        

        public OrganizingMode OrganizingMode => _organizingMode;
        public SortDirection SortDirection => _sortDirection;
        public SwitchThreshold SwitchThreshold => _switchThreshold;
        public HorizontalAlignment HorizontalAlignment => _horizontalAlignment;
        public VerticalAlignment VerticalAlignment => _verticalAlignment;
        public Array AcceptedGroups => _acceptedGroups;
        public bool AllowDroppingFromOtherContainers => _allowDroppingFromOtherContainers;
        public bool RegularSizeChildren => _regularSizeChildren;
        public bool LowPerformance => _lowPerformance;
        public bool NoShifting => _noShifting;
        public float SortAnimDuration => _sortAnimDuration;
        public float Separation => _separation;
        public float SortPrecision => _sortPrecision;
        public bool SeparationOnEnds => _separationOnEnds;
        public bool Wrap => _wrap;
        
        public Vector2 RegularSize => _regularSize;
        public bool IsMouseOver => _isMouseOver;
        public OrganizableItem DraggedChild => _draggedChild;
        public bool HasNonOrganizableChildren => CheckForOrganizableChildren();
        public bool Changed => _changed;
        public Tween Tween => _tween;
        
        public int CurrHoveredIndex => GetCurrHoveredIndex();
        
        public override void _ReadySafe()
        {
            if (_tween == null)
            {
                _tween = new Tween();
                GetParent().CallDeferred("add_child", _tween);
            }
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

            if (GetChildCount() > 0 && RegularSize <= Vector2.Zero)
            {
                Control child = GetChild<Control>(0);
                _regularSize = child.RectSize;
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
            
            Dictionary allowDropping = new Dictionary
            {
                {"name", "dropping/allow_dropping"},
                {"type", Variant.Type.Bool},
            };
            list.Add(allowDropping);

            Dictionary acceptedGroups = new Dictionary
            {
                {"name", "dropping/accepted_groups"},
                {"type", Variant.Type.Array},
            };
            list.Add(acceptedGroups);
            
            Dictionary orgMode = new Dictionary
            {
                {"name", "layout/organizing_mode"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "Horizontal, Vertical"},
            };
            list.Add(orgMode);
            
            Dictionary hAlign = new Dictionary
            {
                {"name", "layout/horizontal_alignment"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "Left, Center, Right"},
            };
            list.Add(hAlign);
            
            Dictionary vAlign = new Dictionary
            {
                {"name", "layout/vertical_alignment"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "Top, Center, Bottom"},
            };
            list.Add(vAlign);
            
            Dictionary sortDir = new Dictionary
            {
                {"name", "layout/sort_direction"},
                {"type", Variant.Type.Int},
                {"hint", PropertyHint.Enum},
                {"hint_string", "Forward, Backward"},
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
            
            Dictionary noShift = new Dictionary
            {
                {"name", "optimizations/disable_shifting"},
                {"type", Variant.Type.Bool},
            };
            list.Add(noShift);
            
            Dictionary sepOnEnds = new Dictionary
            {
                {"name", "layout/separation_on_ends"},
                {"type", Variant.Type.Bool},
            };
            list.Add(sepOnEnds);
            
            Dictionary wrap = new Dictionary
            {
                {"name", "layout/wrap"},
                {"type", Variant.Type.Bool},
            };
            list.Add(wrap);

            return list;
        }

        public override object _Get(string property)
        {
            switch (property)
            {
                case "layout/organizing_mode":
                    return OrganizingMode;
                case "dropping/allow_dropping":
                    return AllowDroppingFromOtherContainers;
                case "dropping/accepted_groups":
                    return AcceptedGroups;
                case "layout/sort_direction":
                    return SortDirection;
                case "behaviour/switch_threshold":
                    return SwitchThreshold;
                case "layout/separation":
                    return Separation;
                case "layout/horizontal_alignment":
                    return HorizontalAlignment;
                case "layout/vertical_alignment":
                    return VerticalAlignment;
                case "behaviour/sort_precision":
                    return SortPrecision;
                case "behaviour/sort_animation_duration":
                    return SortAnimDuration;
                case "optimizations/regular_sized_children":
                    return RegularSizeChildren;
                case "optimizations/low_performance_mode":
                    return LowPerformance;
                case "optimizations/disable_shifting":
                    return NoShifting;
                case "layout/separation_on_ends":
                    return SeparationOnEnds;
                case "layout/wrap":
                    return Wrap;
            }
            
            return base._Get(property);
        }

        public override bool _Set(string property, object value)
        {
            _UpdatePreview();
            
            switch (property)
            {
                case "layout/organizing_mode":
                    _organizingMode = (OrganizingMode) value;
                    _UpdatePreview();
                    return true;
                case "dropping/allow_dropping":
                    _allowDroppingFromOtherContainers = (bool) value;
                    return true;
                case "dropping/accepted_groups":
                    _acceptedGroups = (Array) value;
                    return true;
                case "layout/sort_direction":
                    _sortDirection = (SortDirection) value;
                    _UpdatePreview();
                    return true;
                case "behaviour/switch_threshold":
                    _switchThreshold = (SwitchThreshold) value;
                    return true;
                case "layout/separation":
                    _separation = (float) value;
                    _UpdatePreview();
                    return true;
                case "layout/horizontal_alignment":
                    _horizontalAlignment = (HorizontalAlignment) value;
                    _UpdatePreview();
                    return true;
                case "layout/vertical_alignment":
                    _verticalAlignment = (VerticalAlignment) value;
                    _UpdatePreview();
                    return true;
                case "behaviour/sort_precision":
                    _sortPrecision = (float) value;
                    return true;
                case "behaviour/sort_animation_duration":
                    _sortAnimDuration = (float) value;
                    return true;
                case "optimizations/regular_sized_children":
                    _regularSizeChildren = (bool) value;
                    _UpdatePreview();
                    return true;
                case "optimizations/low_performance_mode":
                    _lowPerformance = (bool) value;
                    return true;
                case "optimizations/disable_shifting":
                    _noShifting = (bool) value;
                    return true;
                case "layout/separation_on_ends":
                    _separationOnEnds = (bool) value;
                    _UpdatePreview();
                    return true;
                case "layout/wrap":
                    _wrap = (bool) value;
                    _UpdatePreview();
                    return true;
            }
            
            return base._Set(property, value);
        }

        public override void _UpdatePreview()
        {
            SetRegularSizedChildren(_regularSizeChildren);
        }

        public override void _ProcessSafe(float delta)
        {
            if (DraggedChild != null && !NoShifting)
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

        public override void _ExitTreeSafe()
        {
            _tween.StopAll();
            _tween.QueueFree();
            _tween = null;
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
            Vector2 mousePos = GetLocalMousePosition();
            
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

                            float thresholdY = GetItemYPosByIndex(item.GetIndex());
                            
                            if (SortDirection == SortDirection.Forward)
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

                                if (mousePos.x > thresholdX)
                                {
                                    continue;
                                }

                                if (mousePos.y > thresholdY && mousePos.y < (thresholdY + item.RectSize.y + Separation))
                                {
                                    return item.GetIndex();
                                }
                            }
                            else if (SortDirection == SortDirection.Backward)
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
                                
                                if (mousePos.y > thresholdY && mousePos.y < (thresholdY + item.RectSize.y))
                                {
                                    return item.GetIndex();
                                }
                            }
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
                    if (node is OrganizableItem item && (!item.IsDragged))
                    {
                        Vector2 newPos = new Vector2(GetItemXPosByIndex(item.GetIndex()), GetItemYPosByIndex(item.GetIndex()));
                                
                        if (!LowPerformance)
                        {
                            _tween.Stop(item, "rect_position");
                        }
                        
                        _tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                            SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                
                        if (!_tween.IsActive())
                        {
                            _tween.Start();
                        }
                    }
                }
            }
        }

        private float GetItemXPosByIndexNoWrap(int idx)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                float currEndX = GetFirstItemXPos();

                if (RegularSizeChildren)
                {
                    if (SortDirection == SortDirection.Forward)
                    {
                        return currEndX + (idx * (RegularSize.x + Separation));
                    }
                    else if (SortDirection == SortDirection.Backward)
                    {
                        return currEndX - (idx * (RegularSize.x + Separation));
                    }
                }
                else
                {
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item && item.GetIndex() <= idx - 1)
                        {
                            if (SortDirection == SortDirection.Forward)
                            {
                                currEndX += (item.RectSize.x + Separation);
                            }
                            else if (SortDirection == SortDirection.Backward)
                            {
                                currEndX -= (item.RectSize.x + Separation);
                            }
                        }
                        else break;
                    }

                    return currEndX;
                }
            }
            else if (OrganizingMode == OrganizingMode.Vertical)
            {
                return 0f; // TODO
            }

            return 0f;
        }

        private float GetItemXPosByIndex(int idx)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (Wrap)
                {
                    int row = GetRowIdxByItemIdx(idx);
                    int idxWithinRow = idx - GetFirstItemIdxInRow(row);
                    
                    return GetItemXPosByIndexNoWrap(row == 0 ? idxWithinRow : idxWithinRow - 1);
                }
                else
                {
                    return GetItemXPosByIndexNoWrap(idx);
                }
            }
            else if (OrganizingMode == OrganizingMode.Vertical)
            {
                return 0f; // TODO
            }

            return 0f;
        }

        public float GetItemYPosByIndex(int idx)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (Wrap)
                {
                    int row = GetRowIdxByItemIdx(idx);
                
                    return ((GetRowHighestItemHeight(row) + Separation) * (row + 1)); 
                }
                else
                {
                    return GetRowHighestItemHeight(0) + Separation; 
                }
            }
            else if (OrganizingMode == OrganizingMode.Vertical)
            {
                float currEndX = GetFirstItemXPos();
            
                if (RegularSizeChildren)
                {
                    if (SortDirection == SortDirection.Forward)
                    {
                        return currEndX + (idx * (RegularSize.y + Separation));
                    }
                    else if (SortDirection == SortDirection.Backward)
                    {
                        return currEndX - (idx * (RegularSize.y + Separation));
                    }
                }
                else
                {
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item && item.GetIndex() <= idx - 1)
                        {
                            if (SortDirection == SortDirection.Forward)
                            {
                                currEndX += (item.RectSize.x + Separation);
                            }
                            else if (SortDirection == SortDirection.Backward)
                            {
                                currEndX -= (item.RectSize.x + Separation);
                            }
                        }
                        else break;
                    }

                    return currEndX;
                }
            }

            return 0f;
        }

        public float GetLastItemXPos(SortDirection dir = SortDirection.Undefined)
        {
            if (dir == SortDirection.Undefined)
            {
                dir = SortDirection;
            }
            
            if (dir == SortDirection.Forward)
            {
                GetFirstItemXPos(SortDirection.Backward);
            }
            else if (dir == SortDirection.Backward)
            {
                GetFirstItemXPos(SortDirection.Forward);
            }

            return -1f;
        }

        public float GetFirstItemXPos(SortDirection dir = SortDirection.Undefined)
        {
            if (dir == SortDirection.Undefined)
            {
                dir = SortDirection;
            }
            
            if (dir == SortDirection.Forward)
            {
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        return SeparationOnEnds ? Separation : 0f;
                    case HorizontalAlignment.Center:
                        return SeparationOnEnds ? Separation + (RectSize.x / 2) - (GetRowMaxLength() / 2) : (RectSize.x / 2) - (GetRowMaxLength() / 2);
                    case HorizontalAlignment.Right:
                        return SeparationOnEnds ? Separation + RectSize.x - GetRowMaxLength() : RectSize.x - GetRowMaxLength();
                }
            }
            else if (dir == SortDirection.Backward)
            {
                float lastSize = (RegularSizeChildren ? RegularSize.x : GetChild<Control>(GetChildCount() - 1).RectSize.x);
                
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        return SeparationOnEnds ? GetRowMaxLength() - lastSize - Separation : GetRowMaxLength() - lastSize;
                    case HorizontalAlignment.Center:
                        return SeparationOnEnds ? (RectSize.x / 2) + (GetRowMaxLength() / 2) - lastSize - Separation : (RectSize.x / 2) + (GetRowMaxLength() / 2) - lastSize;
                    case HorizontalAlignment.Right:
                        return SeparationOnEnds ? RectSize.x - lastSize - Separation : RectSize.x - lastSize;
                }
            }

            return -1f;
        }

        public float GetRowMaxLength()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (RegularSizeChildren)
                {
                    int count = GetChildCount();

                    return SeparationOnEnds ? (RegularSize.x * count) + (Separation * (count + 1)) : (RegularSize.x * count) + (Separation * (count - 1));
                }
                else
                {
                    float length = SeparationOnEnds ? Separation : 0f;
                    
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item)
                        {
                            length += item.RectSize.x + Separation;

                            if (item.GetIndex() == GetChildCount() - 1)
                            {
                                if (!SeparationOnEnds) length -= Separation;
                            }
                        }
                    }

                    return length;
                }
            }
            else if (OrganizingMode == OrganizingMode.Vertical)
            {
                if (RegularSizeChildren)
                {
                    int count = GetChildCount();

                    return SeparationOnEnds ? (RegularSize.y * count) + (Separation * (count + 1)) : (RegularSize.y * count) + (Separation * (count - 1));
                }
                else
                {
                    float length = SeparationOnEnds ? Separation : 0f;
                    
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item)
                        {
                            length += item.RectSize.y + Separation;

                            if (item.GetIndex() == GetChildCount() - 1)
                            {
                                if (!SeparationOnEnds) length -= Separation;
                            }
                        }
                    }

                    return length;
                }
            }

            return -1f;
        }

        public int GetRowCount()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {/*
                if (RegularSizeChildren)
                {
                    return Mathf.FloorToInt(GetChildCount() / GetMaxItemCountForRow());
                }
                else*/
                {
                    int additiveCount = 0;
                    for (int i = 0; i < 1000; i++)
                    {
                        int count = GetMaxItemCountForRow(i);

                        if (count == -1)
                        {
                            return i + 1;
                        }
                        
                        additiveCount += count > -1 ? count : 0;
                        
                        if (additiveCount <= GetChildCount() - 1)
                        {
                            continue;
                        }
                        
                        return i + 1;
                    }
                }
            }

            return -1;
        }

        public float GetRowHighestItemHeight(int rowIdx)
        {
            if (RegularSizeChildren)
            {
                return RegularSize.y;
            }
            
            float highest = 0f;
            
            for (int i = GetFirstItemIdxInRow(rowIdx); i < GetLastItemIdxInRow(rowIdx); i++)
            {
                Control child = GetChild<Control>(i);
                float height = child.RectSize.y;
                
                if (height > highest)
                {
                    highest = height;
                }
            }

            return highest;
        }

        public int GetFirstItemIdxInRow(int rowIdx)
        {
            int currRowLastItemIdx = -1;
            for (int i = 0; i < GetRowCount(); i++)
            {
                if (i == rowIdx)
                {
                    return currRowLastItemIdx != -1 ? currRowLastItemIdx : 0;
                }
                
                currRowLastItemIdx += GetMaxItemCountForRow(i);
            }

            return -1;
        }

        public int GetLastItemIdxInRow(int rowIdx)
        {
            int currRowLastItemIdx = -1;
            for (int i = 0; i < GetRowCount(); i++)
            {
                currRowLastItemIdx += GetMaxItemCountForRow(i);
                
                if (i == rowIdx)
                {
                    return currRowLastItemIdx != -1 ? currRowLastItemIdx : 0;
                }
            }

            return -1;
        }

        public int GetRowIdxByItemIdx(int itemIdx)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (RegularSizeChildren)
                {
                    for (int i = 0; i < GetRowCount(); i++)
                    {
                        if (itemIdx + 1 <= ((i + 1) * GetMaxItemCountForRow()))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    int additiveMaxItemCount = 0;
                    for (int i = 0; i < GetRowCount(); i++)
                    {
                        
                        int currRowItemCount = GetMaxItemCountForRow(i);
                        additiveMaxItemCount += currRowItemCount > -1 ? currRowItemCount : 0;
                        /*
                        if (i == 2)
                        {
                            GD.Print("MAX ITEM FOR ROW = " + currRowItemCount);
                            GD.Print("CURR ADDITIVE COUNT = " + additiveMaxItemCount);
                            GD.Print("ITEM IDX = " + (itemIdx + 1));
                        }
*/
                        if (itemIdx + 1 <= additiveMaxItemCount)
                        {
                            return i;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            return -1;
        }

        public int GetMaxItemCountForRow(int rowIdx = 0)
        {
            int currRow = 0;
            int currAmountOnCurrRow = 1;
            
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (RegularSizeChildren)
                {
                    return Mathf.FloorToInt(RectSize.x / RegularSize.x);
                }
                else
                {
                    float currWidth = SeparationOnEnds ? Separation : 0f;
                    
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item)
                        {
                            currWidth += item.RectSize.x + Separation;

                            if (currWidth >= (SeparationOnEnds ? RectSize.x - Separation : RectSize.x) - (item.RectSize.x * SortPrecision))
                            {
                                if (currRow == rowIdx)
                                {
                                    return currAmountOnCurrRow;
                                }
                                else if (currRow > rowIdx)
                                {
                                    // just a sanity check in case something goes wrong somehow
                                    return currAmountOnCurrRow;
                                }
                                currRow++;
                                currAmountOnCurrRow = 1;
                                currWidth = SeparationOnEnds ? Separation : 0f;
                                continue;
                            }
                            
                            currAmountOnCurrRow++;
                        }
                    }
                    
                    if (currRow == rowIdx)
                    {
                        // this means that we didn't exceed the width limit -> non-full row, so we'll just return curr amount
                        return currAmountOnCurrRow;
                    }
                }
            }
            
            return -1;
        }

        public bool IsSortDone()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && !item.IsDragged)
                    {
                        if (Math.Abs(item.RectPosition.y - GetItemYPosByIndex(item.GetIndex())) > SortPrecision)
                        {
                            return false;
                        }

                        if (Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) > SortPrecision)
                        {
                            return false;
                        }
                    }
                    else if (node is OrganizableItem && DraggedChild != node)
                    {
                        _draggedChild = node as OrganizableItem;
                        return false;
                    }
                }

                return true;
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
                        _tween.Stop(item, "rect_position");
                        _draggedChild = item;
                        return;
                    }
                }
            }
        }
        
        private bool CheckForOrganizableChildren()
        {
            foreach (Node node in GetChildren())
            {
                if (node is OrganizableItem item)
                {
                    continue;
                }
                
                return true;
            }

            return false;
        }

        public void SetRegularSizedChildren(bool value)
        {
            _regularSizeChildren = value;

            bool first = true;
            
            foreach (Node node in GetChildren())
            {
                if (node is OrganizableItem item)
                {
                    if (first)
                    {
                        _regularSize = item.RectSize;
                        first = false;
                        continue;
                    }
                    
                    item.RectSize = RegularSize;
                    return;
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
        Forward,
        Backward,
        Undefined
    }

    public enum SwitchThreshold
    {
        End,
        Middle,
        Start
    }

    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }
}