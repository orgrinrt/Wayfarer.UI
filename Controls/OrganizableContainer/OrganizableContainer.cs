using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
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
        [Export()] private bool _regularSizeChildren = false;
        [Export()] private bool _isMouseOver = false;
        [Export()] private OrganizableItem _draggedChild;
        [Export()] private bool _hasNonOrganizableChildren = false;
        [Export()] private float _sortAnimDuration = 0.4f;
        [Export()] private float _separation = 5f;
        [Export()] private float _sortPrecision = 0.5f;
        [Export()] private float _axisAnchor = -999999f;
        [Export()] private float _hoverShift = 15f;
        

        public OrganizingMode OrganizingMode => _organizingMode;
        public SortDirection SortDirection => _sortDirection;
        public bool RegularSizeChildren => _regularSizeChildren;
        public bool IsMouseOver => _isMouseOver;
        public OrganizableItem DraggedChild => _draggedChild;
        public bool HasNonOrganizableChildren => _hasNonOrganizableChildren;
        public int CurrHoveredIndex => GetCurrHoveredIndex();
        public float SortAnimDuration => _sortAnimDuration;
        public float Separation => _separation;
        public float SortPrecision => _sortPrecision;
        public float AxisAnchor => _axisAnchor;
        public float HoverShift => _hoverShift;
        
        public override void _ReadySafe()
        {
            Connect("mouse_entered", this, nameof(OnMouseEntered));
            Connect("mouse_exited", this, nameof(OnMouseExited));
            
            try
            {
                Connections.Add(MouseManager, nameof(MouseManager.StoppedDragging), this, nameof(OnGlobalDragStopped));
            }
            catch (Exception e)
            {
                Connections.Add(GetMouseManager, nameof(MouseManager.StoppedDragging), this, nameof(OnGlobalDragStopped));
            }

            if (GetChildCount() > 0 && Math.Abs(_axisAnchor - (-999999f)) < SortPrecision)
            {
                if (OrganizingMode == OrganizingMode.Horizontal)
                {
                    _axisAnchor = GetChild<Control>(0).RectPosition.y;
                }
                else if (OrganizingMode == OrganizingMode.Vertical)
                {
                    _axisAnchor = GetChild<Control>(0).RectPosition.x;
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

        public override void _ProcessSafe(float delta)
        {
            if (HasNonOrganizableChildren)
            {
                _hasNonOrganizableChildren = false;
            }
            
            if (DraggedChild != null)
            {
                int hover = GetCurrHoveredIndex();
                if (DraggedChild.GetIndex() != hover)
                {
                    MoveChild(DraggedChild, hover);
                }
            }
            
            if (!IsSortDone())
            {
                SortAll();
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
                if (RegularSizeChildren)
                {
                    // we can do a simple math calc for optimization
                }
                else
                {
                    foreach (Node node in GetChildren())
                    {
                        if (node is OrganizableItem item)
                        {
                            if (DraggedChild == null && item.IsDragged)
                            {
                                _draggedChild = item;
                            }
                            
                            if (SortDirection == SortDirection.Right)
                            {
                                float itemCenterX = GetItemXPosByIndex(item.GetIndex()) + (item.RectSize.x * 1f);
                                
                                if (GetLocalMousePosition().x > itemCenterX)
                                {
                                    continue;
                                }
                                
                                return item.GetIndex();
                            }
                            else if (SortDirection == SortDirection.Left)
                            {
                                float itemCenterX = GetItemXPosByIndex(item.GetIndex()) + (item.RectSize.x * 1f);
                                if (GetLocalMousePosition().x < itemCenterX)
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
                        if (item.Tween == null)
                        {
                            item.CreateTween();
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
                // we can do simple math for optimization
                return 0f;
            }
            else
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
        }

        public bool IsSortDone()
        {
            if (OrganizingMode == OrganizingMode.Horizontal)
            {
                if (RegularSizeChildren)
                {
                    // again we can do simple math for optimization
                }
                else
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