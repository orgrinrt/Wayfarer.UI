using System;
using System.ComponentModel;
using Godot;
using Wayfarer.Core.Systems.Managers;
using Wayfarer.Editor;
using Wayfarer.Utils.Debug;

namespace Wayfarer.UI.Controls
{
    #if TOOLS
    [Tool]
    #endif
    public class OrganizableContainer : WayfarerControl
    {
        [Export(PropertyHint.Enum, "Horizontal, Vertical, Grid")] private OrganizingMode _organizingMode = OrganizingMode.Horizontal;
        [Export(PropertyHint.Enum, "Right, Left")] private SortDirection _sortDirection = SortDirection.Right;
        [Export()] private bool _isChildDragged = false;
        [Export()] private bool _hasNonOrganizableChildren = false;
        [Export()] private int _currHoveredIndex = 0;
        [Export()] private int _prevHoveredIndex = 0;
        [Export()] private float _sortAnimDuration = 1f;
        [Export()] private float _separation = 5f;
        [Export()] private float _sortPrecision = 0.5f;

        public OrganizingMode OrganizingMode => _organizingMode;
        public SortDirection SortDirection => _sortDirection;
        public bool IsChildDragged => _isChildDragged;
        public bool HasNonOrganizableChildren => _hasNonOrganizableChildren;
        public int CurrHoveredIndex => _currHoveredIndex;
        public int PrevHoveredIndex => _prevHoveredIndex;
        public float SortAnimDuration => _sortAnimDuration;
        public float Separation => _separation;
        public float SortPrecision => _sortPrecision;
        
        public override void _ReadySafe()
        {
            Connect("mouse_entered", this, nameof(OnMouseEntered));
            
            try
            {
                Connections.Add(MouseManager, nameof(MouseManager.StoppedDragging), this, nameof(OnGlobalDragStopped));
            }
            catch (Exception e)
            {
                Connections.Add(GetMouseManager, nameof(MouseManager.StoppedDragging), this, nameof(OnGlobalDragStopped));
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
            
            return ""
                ;
        }

        public override void _ProcessSafe(float delta)
        {
            _hasNonOrganizableChildren = false;
            
            if (IsChildDragged && HoveredIndexChanged())
            {
                SlideOthersOnHover(CurrHoveredIndex);
            }
            else if (!IsSortDone())
            {
                SortX();
            }
        }

        private bool HoveredIndexChanged()
        {
            if (IsChildDragged && OrganizingMode == OrganizingMode.Horizontal)
            {
                _prevHoveredIndex = _currHoveredIndex;
                
                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && !item.IsDragged)
                    {
                        if (SortDirection == SortDirection.Right)
                        {
                            float itemCenterXGlobal = item.RectGlobalPosition.x + (item.RectSize.x / 2);
                            if (GetGlobalMousePosition().x > itemCenterXGlobal)
                            {
                                continue;
                            }
                            else
                            {
                                int newIndex = item.GetIndex();
                                if (_prevHoveredIndex != newIndex)
                                {
                                    _currHoveredIndex = newIndex;
                                    return true;
                                }
                            }
                        }
                        else if (SortDirection == SortDirection.Left)
                        {
                            float itemCenterXGlobal = item.RectGlobalPosition.x + (item.RectSize.x / 2);
                            if (GetGlobalMousePosition().x < itemCenterXGlobal)
                            {
                                continue;
                            }
                            else
                            {
                                int newIndex = item.GetIndex();
                                if (_prevHoveredIndex != newIndex)
                                {
                                    _currHoveredIndex = newIndex;
                                    return true;
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
                        _isChildDragged = true;
                    }
                }
            }

            return false;
        }

        private void SlideOthersOnHover(int hoverIdx)
        {
            if (IsChildDragged && OrganizingMode == OrganizingMode.Horizontal)
            {
                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && !item.IsDragged)
                    {
                        if (item.Tween == null)
                        {
                            item.CreateTween();
                        }
                        
                        if (SortDirection == SortDirection.Right)
                        {
                            if (item.GetIndex() > hoverIdx && Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) < item.RectSize.x)
                            {
                                Vector2 newPos = new Vector2(item.RectPosition.x + item.RectSize.x, item.RectPosition.y);
                                item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                                    SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                item.SetTargetPos(newPos);
                                item.Tween.Start();
                            }
                        }
                        else if (SortDirection == SortDirection.Left)
                        {
                            if (item.GetIndex() > hoverIdx && Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) < item.RectSize.x)
                            {
                                Vector2 newPos = new Vector2(item.RectPosition.x - item.RectSize.x, item.RectPosition.y);
                                item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                                    SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                item.SetTargetPos(newPos);
                                item.Tween.Start();
                            }
                        } 
                    }
                    else if (!(node is OrganizableItem))
                    {
                        _hasNonOrganizableChildren = true;
                    }
                    else
                    {
                        _isChildDragged = true;
                    }
                }
            }
        }

        private void SortX()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && !item.IsDragged)
                    {
                        if (item.Tween == null)
                        {
                            item.CreateTween();
                        }
                        
                        if (SortDirection == SortDirection.Right)
                        {
                            if (!item.Tween.IsActive() && Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) > SortPrecision)
                            {
                                Vector2 newPos = new Vector2(GetItemXPosByIndex(item.GetIndex()), item.RectPosition.y);
                                item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                                    SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                item.SetTargetPos(newPos);
                                item.Tween.Start();
                            }
                        }
                        else if (SortDirection == SortDirection.Left)
                        {
                            if (!item.Tween.IsActive() && Math.Abs(item.RectPosition.x - GetItemXPosByIndex(item.GetIndex())) > SortPrecision)
                            {
                                Vector2 newPos = new Vector2(GetItemXPosByIndex(item.GetIndex()), item.RectPosition.y);
                                item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                                    SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                                item.SetTargetPos(newPos);
                                item.Tween.Start();
                            }
                        }
                    }
                    else if (!(node is OrganizableItem))
                    {
                        _hasNonOrganizableChildren = true;
                    }
                    else
                    {
                        _isChildDragged = true;
                    }
                }
            }
        }

        private void SortY(OrganizableItem item)
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (item.Tween == null)
                {
                    item.CreateTween();
                }
                
                int neighbourIdx = item.GetIndex() + 1;
        
                if (GetChildCount() == item.GetIndex() + 1)
                {
                    neighbourIdx = item.GetIndex() - 1;
                }

                OrganizableItem neighbor = GetChild<OrganizableItem>(neighbourIdx);

                float y = neighbor.RectPosition.y;

                if (!neighbor.IsDragged && Math.Abs(item.RectPosition.y - y) > SortPrecision)
                {
                    Vector2 newPos = new Vector2(GetItemXPosByIndex(item.GetIndex()), y);
                    item.Tween.InterpolateProperty(item, "rect_position", item.RectPosition, newPos,
                        SortAnimDuration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                    item.Tween.Start();
                }
            }
        }

        private float GetItemXPosByIndex(int idx)
        {
            float currEndX = SortDirection == SortDirection.Right ? 0f : (RectSize.x - GetChild<Control>(GetChildCount()-1).RectSize.x);

            foreach (Node node in GetChildren())
            {
                if (node is OrganizableItem item && item.GetIndex() <= idx - 1)
                {
                    currEndX += (item.RectSize.x + Separation);
                }
                else break;
            }

            return currEndX;
        }

        private bool IsSortDone()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                float currEndX = SortDirection == SortDirection.Right ? 0f : RectSize.x;

                foreach (Node node in GetChildren())
                {
                    if (node is OrganizableItem item && !item.IsDragged)
                    {
                        int nextIdx = item.GetIndex() + 1;

                        if (nextIdx == GetChildCount())
                        {
                            return true;
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
                        _isChildDragged = true;
                    }
                    
                    continue;
                }
            }
            
            return false;
        }

        private void OnMouseEntered()
        {
            if (IsChildDragged)
            {
                
            }
        }

        private void OnGlobalDragStopped(Node node)
        {
            _isChildDragged = false;

            if (node is OrganizableItem item)
            {
                foreach (Node child in GetChildren())
                {
                    if (item == child)
                    {
                        MoveChild(item, CurrHoveredIndex);
                        SortY(item);
                    }
                }
            }
        }
    }
    
    public enum OrganizingMode
    {
        Vertical,
        Horizontal,
        Grid
    }

    public enum SortDirection
    {
        Right,
        Left
    }
}