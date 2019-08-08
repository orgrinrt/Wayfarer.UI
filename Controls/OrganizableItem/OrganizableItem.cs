using System;
using Godot;
using Wayfarer.Core;
using Wayfarer.Core.Systems.Managers;
using Wayfarer.Editor;
using Wayfarer.Utils.Debug;

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
        private MouseManager _mouseManager;
        private Vector2 _targetPos;

        public Tween Tween => _tween;
        public bool MouseOver => _mouseOver;
        public bool IsDragged => _isDragged;
        public MouseManager MouseManager => GetMouseManager();
        public Vector2 TargetPos => _targetPos;

        private bool _isNotConnected = false;
        
        public override void _EnterTree()
        {
            base._EnterTree();

            CreateTween();
        }

        public override void _Ready()
        {
            base._Ready();
            
            Connect("mouse_entered", this, nameof(OnMouseEntered));
            Connect("mouse_exited", this, nameof(OnMouseExited));
            
            try
            {
                MouseManager.Connect(nameof(MouseManager.StoppedDragging), this, nameof(EndDrag));
               
            }
            catch (Exception e)
            {
                Log.Error("Couldn't connect to MouseManager's EndDrag() signal", e, true);
                _isNotConnected = true;
            }
            
            SetDefaultCursorShape(CursorShape.Drag);
        }
/*
        public override void _Notification(int what)
        {
            if (what == NotificationMouseEnter)
            {
                OnMouseEntered();
            }
            else if (what == NotificationMouseExit)
            {
                OnMouseExited();
            }
        }*/

        public override void _Process(float delta)
        {
            base._Process(delta);

            if (_isNotConnected)
            {
                _isNotConnected = false;
                
                try
                {
                    if (!MouseManager.IsConnected(nameof(MouseManager.StoppedDragging), this, nameof(EndDrag)))
                    {
                        MouseManager.Connect(nameof(MouseManager.StoppedDragging), this, nameof(EndDrag));
                    }
                    else
                    {
                        _isNotConnected = false;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Couldn't connect to MouseManager's EndDrag() signal", e, true);
                    _isNotConnected = true;
                }
            }
            
            if (MouseOver && !IsDragged)
            {
                if (Input.IsMouseButtonPressed((int) ButtonList.Left))
                {
                    StartDrag();
                }
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            _tween = null;
            _mouseManager = null;
        }
        
        public void StartDrag()
        {
            MouseManager.StartDragging(this, GetLocalMousePosition());
            _isDragged = true;
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

        public MouseManager GetMouseManager()
        {
            if (IsInstanceValid(_mouseManager) || _mouseManager == null)
            {
                #if TOOLS 
                // it's likely this logic doesn't work so we better have an export bool to explicitly set if this is part of editor
                _mouseManager = WayfarerEditorPlugin.Instance.MouseManager;
                return _mouseManager;
                #endif
            
                _mouseManager = Game.MouseManager;
            }

            return _mouseManager;
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