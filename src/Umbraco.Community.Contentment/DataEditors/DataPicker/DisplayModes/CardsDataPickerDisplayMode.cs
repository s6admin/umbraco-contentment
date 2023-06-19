﻿/* Copyright © 2023 Lee Kelleher.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
#if NET472
using Umbraco.Core.PropertyEditors;
#else
using Umbraco.Cms.Core.PropertyEditors;
#endif

namespace Umbraco.Community.Contentment.DataEditors
{
    internal class CardsDataPickerDisplayMode : IDataPickerDisplayMode
    {
        public string Name => "Cards";

        public string Description => "Items will be displayed as cards.";

        public string Icon => "icon-playing-cards";

        public string Group => default;

        public string View => Constants.Internals.EditorsPathRoot + "data-picker.html";

        public Dictionary<string, object> DefaultValues => default;

        public Dictionary<string, object> DefaultConfig => new Dictionary<string, object>
        {
            { "displayMode", "cards" },
        };

        public IEnumerable<ConfigurationField> Fields => default;

        public OverlaySize OverlaySize => OverlaySize.Small;
    }
}
