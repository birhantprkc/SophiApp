// <copyright file="UIModelDto.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Models
{
    using SophiApp.Helpers;

    /// <summary>
    /// Data transfer object for UI model.
    /// </summary>
    public class UIModelDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIModelDto"/> class.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <param name="type">Model type.</param>
        /// <param name="tag">Model tag.</param>
        /// <param name="viewId">Model view id.</param>
        /// <param name="windows10Support">Model supported Windows 10.</param>
        /// <param name="windows11Support">Model supported Windows 11.</param>
        /// <param name="numberOfItems">Number of child items.</param>
        public UIModelDto(string name, UIModelType type, UICategoryTag tag, int viewId, bool windows10Support, bool windows11Support, int numberOfItems)
        {
            Name = name;
            Type = type;
            Tag = tag;
            ViewId = viewId;
            Windows10Support = windows10Support;
            Windows11Support = windows11Support;
            NumberOfItems = numberOfItems;
        }

        /// <summary>
        /// Gets a model name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets a model type.
        /// </summary>
        public UIModelType Type { get; init; }

        /// <summary>
        /// Gets category tag.
        /// </summary>
        public UICategoryTag Tag { get; init; }

        /// <summary>
        /// Gets a model view id.
        /// </summary>
        public int ViewId { get; init; }

        /// <summary>
        /// Gets a value indicating whether Windows 10 support.
        /// </summary>
        public bool Windows10Support { get; init; }

        /// <summary>
        /// Gets a value indicating whether Windows 11 support.
        /// </summary>
        public bool Windows11Support { get; init; }

        /// <summary>
        /// Gets a number of child items.
        /// </summary>
        public int NumberOfItems { get; init; }
    }
}
