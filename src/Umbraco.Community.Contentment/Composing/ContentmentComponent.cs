﻿/* Copyright © 2019 Lee Kelleher.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

#if NET472
using System.Collections.Generic;
using Umbraco.Community.Contentment.Migrations;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Models;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web.JavaScript;
using Umbraco.Web.Models.ContentEditing;

namespace Umbraco.Community.Contentment.Composing
{
    internal sealed class ContentmentComponent : IComponent
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMigrationBuilder _migrationBuilder;
        private readonly IKeyValueService _keyValueService;
        private readonly IProfilingLogger _logger;

        public ContentmentComponent(
            IScopeProvider scopeProvider,
            IMigrationBuilder migrationBuilder,
            IKeyValueService keyValueService,
            IProfilingLogger logger)
        {
            _scopeProvider = scopeProvider;
            _migrationBuilder = migrationBuilder;
            _keyValueService = keyValueService;
            _logger = logger;
        }

        public void Initialize()
        {
            var upgrader = new Upgrader(new ContentmentPlan());
            upgrader.Execute(_scopeProvider, _migrationBuilder, _keyValueService, _logger);

            DataTypeService.Deleted += DataTypeService_Deleted;
            DataTypeService.Saved += DataTypeService_Saved;
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;
        }

        public void Terminate()
        {
            DataTypeService.Deleted -= DataTypeService_Deleted;
            DataTypeService.Saved -= DataTypeService_Saved;
            ServerVariablesParser.Parsing -= ServerVariablesParser_Parsing;
        }

        private void DataTypeService_Deleted(IDataTypeService sender, DeleteEventArgs<IDataType> e)
        {
            foreach (var entity in e.DeletedEntities)
            {
                DataEditors.DataPickerApiController.ClearCache(entity.Key);
            }
        }

        private void DataTypeService_Saved(IDataTypeService sender, SaveEventArgs<IDataType> e)
        {
            foreach (var entity in e.SavedEntities)
            {
                DataEditors.DataPickerApiController.ClearCache(entity.Key);
            }
        }

        private void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (e.TryGetValueAs("umbracoPlugins", out Dictionary<string, object> umbracoPlugins) == true && umbracoPlugins.ContainsKey(Constants.Internals.ProjectAlias) == false)
            {
                umbracoPlugins.Add(Constants.Internals.ProjectAlias, new
                {
                    name = Constants.Internals.ProjectName,
                    version = ContentmentVersion.SemanticVersion.ToSemanticString(),
                    telemetry = Telemetry.ContentmentTelemetryComponent.Disabled == false,
                });
            }
        }
    }
}
#endif
