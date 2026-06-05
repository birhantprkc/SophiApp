// <copyright file="FeatureExperiencePackRemovedPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="FeatureExperiencePackRemovedPage"/>.
    /// </summary>
    public sealed partial class FeatureExperiencePackRemovedPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureExperiencePackRemovedPage"/> class.
        /// </summary>
        public FeatureExperiencePackRemovedPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<FeatureExperiencePackRemovedViewModel>();
        }

        /// <summary>
        /// Gets <see cref="FeatureExperiencePackRemovedViewModel"/>.
        /// </summary>
        public FeatureExperiencePackRemovedViewModel ViewModel { get; }
    }
}
