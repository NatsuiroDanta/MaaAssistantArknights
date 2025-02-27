// <copyright file="FightSettingsUserControl.xaml.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// ParamSettingUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class FightSettingsUserControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FightSettingsUserControl"/> class.
        /// </summary>
        public FightSettingsUserControl()
        {
            InitializeComponent();
        }

        private static readonly MethodInfo SetText = typeof(HandyControl.Controls.NumericUpDown).GetMethod("SetText", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly object[] paras = new object[] { true };

        private void NumericUpDown_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
        {
            SetText?.Invoke(sender, paras);
        }
    }
}
