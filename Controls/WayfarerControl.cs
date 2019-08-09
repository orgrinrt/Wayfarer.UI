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

        [Export()] private string _nameOfRoot;
        
        private SignalConnectionHandler _connections = new SignalConnectionHandler();
        private MouseManager _mouseManager;
        private bool _cachedHasTabContainerParent = false;
        
        public SignalConnectionHandler Connections => _connections;
        public MouseManager MouseManager => GetMouseManager();
        public bool CachedHasTabContainerParent => _cachedHasTabContainerParent;
        
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

            if (HasATabContainerParent())
            {
                _cachedHasTabContainerParent = true;
                return;
            }
            else
            {
                _cachedHasTabContainerParent = false;
            }
            
            base._EnterTree();
            _EnterTreeSafe();
        }

        public override void _Ready()
        {
            #if TOOLS
            if (_cachedResetOnReady) return;
            #endif
            
            if (CachedHasTabContainerParent)
            {
                return;
            }
            
            base._Ready();
            this.SetupWayfarer();

            _PreReadySafe();
            _ReadySafe();
        }

        public override void _Process(float delta)
        {
            #if TOOLS
            if (_cachedResetOnReady) return;
            #endif
            
            if (CachedHasTabContainerParent)
            {
                return;
            }
            
            base._Process(delta);
            Connections?.Update();
            _ProcessSafe(delta);
        }

        public override void _ExitTree()
        {
            #if TOOLS
            if (_cachedResetOnReady) return;
            #endif    
            
            if (CachedHasTabContainerParent)
            {
                return;
            }
            
            base._ExitTree();
            _ExitTreeSafe();
            _mouseManager = null;
            _connections = null;
        }

        public virtual void _EnterTreeSafe()
        {
            
        }

        public virtual void _ReadySafe()
        {
            
        }

        public virtual void _PreReadySafe()
        {
            Connections.Update();
        }

        public virtual void _ProcessSafe(float delta)
        {
            
        }

        public virtual void _ExitTreeSafe()
        {
            
        }

        private bool HasATabContainerParent()
        {
            if (this.GetParentOfType<TabContainer>() != null)
            {
                return true;
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