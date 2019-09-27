﻿/* Copyright © 2019 Lee Kelleher, Umbrella Inc and other contributors.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Umbraco.Community.Contentment.DataEditors;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Umbraco.Community.Contentment.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    internal class ContentmentComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition
                .Components()
                    .Append<ContentmentComponent>()
            ;

            composition
                .DataEditors()
                    .Exclude<CardsDataEditor>()
                    .Exclude<CascadingDropdownListDataEditor>()
                    .Exclude<CheckboxDataEditor>()
                    .Exclude<CheckboxListDataEditor>()
                    .Exclude<CodeEditorDataEditor>()
                    .Exclude<ConfigurationEditorDataEditor>()
                    .Exclude<DataTableDataEditor>()
                    .Exclude<DropdownListDataEditor>()
                    .Exclude<ElementDataEditor>()
                    .Exclude<ItemPickerDataEditor>()
                    .Exclude<MacroPickerDataEditor>()
                    .Exclude<RadioButtonListDataEditor>()
                    .Exclude<TogglesDataEditor>()
            ;
        }
    }
}