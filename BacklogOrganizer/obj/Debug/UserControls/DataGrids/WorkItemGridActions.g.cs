#pragma checksum "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "FE27ED273232316A6982D9626DF40F5CC31AA38C8860F12A90C23128633F162D"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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


namespace BacklogOrganizer.UserControls.DataGrids {
    
    
    /// <summary>
    /// WorkItemGridActions
    /// </summary>
    public partial class WorkItemGridActions : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 11 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button AddTaskButton;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SetReminderButton;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ToggleActiveStateButton;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button EditButton;
        
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
            System.Uri resourceLocater = new System.Uri("/BacklogOrganizer;component/usercontrols/datagrids/workitemgridactions.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
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
            this.AddTaskButton = ((System.Windows.Controls.Button)(target));
            
            #line 12 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
            this.AddTaskButton.Click += new System.Windows.RoutedEventHandler(this.AddTaskButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 2:
            this.SetReminderButton = ((System.Windows.Controls.Button)(target));
            
            #line 16 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
            this.SetReminderButton.Click += new System.Windows.RoutedEventHandler(this.SetReminderButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 3:
            this.ToggleActiveStateButton = ((System.Windows.Controls.Button)(target));
            
            #line 20 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
            this.ToggleActiveStateButton.Click += new System.Windows.RoutedEventHandler(this.ToggleActiveStateButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 4:
            this.EditButton = ((System.Windows.Controls.Button)(target));
            
            #line 24 "..\..\..\..\UserControls\DataGrids\WorkItemGridActions.xaml"
            this.EditButton.Click += new System.Windows.RoutedEventHandler(this.EditButton_OnClick);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

