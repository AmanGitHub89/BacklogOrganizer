﻿#pragma checksum "..\..\..\Windows\AddRemoveItemsWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "4D3BA7FD9F22AAF81C6CB41E8A8DF4D3646771C4A4ED387F7491F1F8935C048C"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using BacklogOrganizer.Types;
using BacklogOrganizer.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace BacklogOrganizer.Windows {
    
    
    /// <summary>
    /// AddRemoveItemsWindow
    /// </summary>
    public partial class AddRemoveItemsWindow : BacklogOrganizer.Types.FixedSizeChildWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 17 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox AllItemsListBox;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ToggleButton;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox SelectedItemsListBox;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/BacklogOrganizer;component/windows/addremoveitemswindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.AllItemsListBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 17 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
            this.AllItemsListBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.AllItemsListBox_OnSelectionChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.ToggleButton = ((System.Windows.Controls.Button)(target));
            
            #line 22 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
            this.ToggleButton.Click += new System.Windows.RoutedEventHandler(this.ToggleButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 3:
            this.SelectedItemsListBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 29 "..\..\..\Windows\AddRemoveItemsWindow.xaml"
            this.SelectedItemsListBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.SelectedItemsListBox_OnSelectionChanged);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

