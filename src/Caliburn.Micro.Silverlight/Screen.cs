﻿namespace Caliburn.Micro
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    public class Screen : PropertyChangedBase, IScreen, IChild<IConductor>, IViewAware
    {
        protected static readonly ILog Log = LogManager.GetLog(typeof(Screen));

        bool isActive;
        bool isInitialized;
        IConductor parent;
        string displayName;
        readonly Dictionary<object, object> views = new Dictionary<object, object>();

        public Screen()
        {
            DisplayName = GetType().FullName;
        }

        public IConductor Parent
        {
            get { return parent; }
            set
            {
                parent = value;
                NotifyOfPropertyChange(() => Parent);
            }
        }

        public string DisplayName
        {
            get { return displayName; }
            set
            {
                displayName = value;
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public bool IsActive
        {
            get { return isActive; }
            private set
            {
                isActive = value;
                NotifyOfPropertyChange(() => IsActive);
            }
        }

        public bool IsInitialized
        {
            get { return isInitialized; }
            private set
            {
                isInitialized = value;
                NotifyOfPropertyChange(() => IsInitialized);
            }
        }

        public event EventHandler<ActivationEventArgs> Activated = delegate { };
        public event EventHandler<DeactivationEventArgs> Deactivated = delegate { };

        void IActivate.Activate()
        {
            if (IsActive)
                return;

            bool initialized = false;

            if(!IsInitialized)
            {
                IsInitialized = initialized = true;
                OnInitialize();
            }

            IsActive = true;
            Log.Info("Activating {0}.", this);
            OnActivate();

            Activated(this, new ActivationEventArgs { WasInitialized = initialized });
        }

        protected virtual void OnInitialize() {}
        protected virtual void OnActivate() {}

        void IDeactivate.Deactivate(bool close)
        {
            if (!IsActive)
                return;

            IsActive = false;
            Log.Info("Deactivating {0}.", this);
            OnDeactivate(close);

            if(close)
                Log.Info("Closed {0}.", this);

            Deactivated(this, new DeactivationEventArgs { WasClosed = close });
        }

        protected virtual void OnDeactivate(bool close) {}

        public virtual void CanClose(Action<bool> callback)
        {
            callback(true);
        }

        public virtual void AttachView(object view, object context = null)
        {
            views[context ?? ViewLocator.DefaultContext] = view;

            var element = view as FrameworkElement;
            if (element != null)
                element.Loaded += delegate { OnViewLoaded(view); };
        }

        protected virtual void OnViewLoaded(object view) { }

        public virtual object GetView(object context = null)
        {
            object view;
            views.TryGetValue(context ?? ViewLocator.DefaultContext, out view);
            return view;
        }

        public void TryClose()
        {
            if (Parent != null)
                Parent.CloseItem(this);
            else
            {
                var view = GetView();

                if (view == null)
                {
                    var ex = new NotSupportedException(
                        "You cannot close an instance without a parent or a default view."
                        );
                    Log.Error(ex);
                    throw ex;
                }

                var method = view.GetType().GetMethod("Close");
                if (method != null)
                {
                    method.Invoke(view, null);
                    return;
                }

                var property = view.GetType().GetProperty("IsOpen");
                if (property != null)
                {
                    property.SetValue(view, false, new object[] { });
                    return;
                }

                var ex2 = new NotSupportedException(
                    "The default view does not support the Close method or the IsOpen property."
                    );

                Log.Error(ex2);
                throw ex2;
            }
        }
    }
}