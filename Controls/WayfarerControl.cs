using System;
using Godot;
using Wayfarer.Core;
using Wayfarer.Core.Systems.Managers;
using Wayfarer.Editor;
using Wayfarer.ModuleSystem;
using Wayfarer.NodeSystem;
using Wayfarer.Utils.Debug;
using Wayfarer.Utils.Helpers;

namespace Wayfarer.UI.Controls
{
    public class WayfarerControl : Control, ISignalConnectionHandled
    {
        private SignalConnectionHandler _connections = new SignalConnectionHandler();
        private MouseManager _mouseManager;
        private bool _cachedIsEditedScene = false;
        private int _cachedChildCount = 0;
        
        public SignalConnectionHandler Connections => _connections;
        public MouseManager MouseManager => GetMouseManager();
        public bool CachedIsEditedScene => _cachedIsEditedScene;
        
        #if TOOLS
        public bool ResetOnReady => WayfarerProjectSettings.ResetOnReady;

        private bool _cachedResetOnReady = true;
        #endif

        public override void _EnterTree()
        {
            #if TOOLS
            _cachedResetOnReady = ResetOnReady;
            if (_cachedResetOnReady) return;
            #endif

            if (IsEditedScene())
            {
                _cachedIsEditedScene = true;
                return;
            }
            else
            {
                _cachedIsEditedScene = false;
            }
            
            base._EnterTree();
            _EnterTreeSafe();
        }

        public override void _Ready()
        {
            #if TOOLS
            if (_cachedResetOnReady) return;
            #endif

            _cachedChildCount = GetChildCount();
            
            if (CachedIsEditedScene)
            {
                return;
            }
            
            base._Ready();
            this.SetupWayfarer();

            _PreReadySafe();
            Connections?.Update();
            _ReadySafe();
        }

        public override void _Process(float delta)
        {
            #if TOOLS
            if (_cachedResetOnReady) return;
            #endif
            
            base._Process(delta);
            Connections?.Update();
            _ProcessSafe(delta);
        }

        public override void _ExitTree()
        {
            #if TOOLS
            if (_cachedResetOnReady) return;
            #endif    
            
            if (CachedIsEditedScene)
            {
                return;
            }
            
            base._ExitTree();
            _ExitTreeSafe();
            _mouseManager = null;
            _connections = null;
        }

        public override object _Get(string property)
        {
            _UpdatePreview();
            return base._Get(property);
        }
        
        public override bool _Set(string property, object value)
        {
            _UpdatePreview();
            return base._Set(property, value);
        }

        public virtual void _EnterTreeSafe()
        {
            
        }

        public virtual void _ReadySafe()
        {
            
        }

        public virtual void _PreReadySafe()
        {
            
        }

        public virtual void _UpdatePreview()
        {
            
        }

        public virtual void _ProcessSafe(float delta)
        {
            
        }

        public virtual void _ExitTreeSafe()
        {
            
        }

        private bool IsEditedScene()
        {
            TabContainer tab = this.GetParentOfType<TabContainer>();
            if (tab != null)
            {
                foreach (Node child in tab.GetChildren())
                {
                    if (child.Name == "Scene") return true;
                }
            }

            return false;
        }

        public MouseManager GetMouseManager()
        {
            if (!IsInstanceValid(_mouseManager) || _mouseManager == null)
            {
                try
                {
                    #if TOOLS 
                    // it's likely this logic doesn't work so we better have an export bool to explicitly set if this is part of editor
                    _mouseManager = WayfarerEditorPlugin.Instance.MouseManager;
                    return _mouseManager;
                    #endif
            
                    _mouseManager = Game.MouseManager;
                }
                catch (Exception e)
                {
                    Log.Wf.Error("Couldn't get MouseManager from the EditorPlugin singleton", e, true);
                }
            }
            
            return _mouseManager;
        }
        
    }
}