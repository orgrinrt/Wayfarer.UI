using System;
using Godot;
using Wayfarer.Core;
using Wayfarer.Core.Systems.Managers;
using Wayfarer.Editor;
using Wayfarer.Utils.Debug;
using Wayfarer.Utils.Debug.Exceptions;

namespace Wayfarer.UI.Controls
{
    #if TOOLS
    [Tool]
    #endif
    public class OrganizableItem : WayfarerControl, IDraggableControl
    {
        private Tween _tween;
        private bool _mouseOver = false;
        private bool _isDragged = false;
        private Vector2 _targetPos;

        public Tween Tween => _tween;
        public bool MouseOver => _mouseOver;
        public bool IsDragged => _isDragged;
        public Vector2 TargetPos => _targetPos;

        public override void _EnterTreeSafe()
        {
            CreateTween();
        }
        
        public override void _PreReadySafe()
        {
            Connect("mouse_entered", this, nameof(OnMouseEntered));
            Connect("mouse_exited", this, nameof(OnMouseExited));

            try
            {
                Connections.Add(MouseManager, nameof(MouseManager.StoppedDragging), this, nameof(EndDrag));
            }
            catch (Exception e)
            {
                Connections.Add(GetMouseManager, nameof(MouseManager.StoppedDragging), this, nameof(EndDrag));
            }
            
        }

        public override void _ReadySafe()
        {
            SetDefaultCursorShape(CursorShape.Drag);
        }

        public override void _ProcessSafe(float delta)
        {
            if (MouseOver && !IsDragged)
            {
                if (Input.IsMouseButtonPressed((int) ButtonList.Left))
                {
                    StartDrag();
                }
            }
        }

        public override void _ExitTreeSafe()
        {
            _tween = null;
        }
        
        public void StartDrag()
        {
            _isDragged = true;
            MouseManager.StartDragging(this, GetLocalMousePosition());
        }

        public void EndDrag(Node node)
        {
            if (node == this)
            {
                _isDragged = false;
            }
        }

        public void CreateTween()
        {
            foreach (Node node in GetChildren())
            {
                if (node is Tween currTween)
                {
                    currTween.StopAll();
                    currTween.Name = "BeingFreed";
                    currTween.QueueFree();
                }
            }
            
            _tween = new Tween { Name = "Tween" };
            
            AddChild(_tween);
        }

        public void SetTargetPos(Vector2 pos)
        {
            _targetPos = pos;
        }

        private void OnMouseEntered()
        {
            _mouseOver = true;
        }

        private void OnMouseExited()
        {
            _mouseOver = false;
        }
    }
}