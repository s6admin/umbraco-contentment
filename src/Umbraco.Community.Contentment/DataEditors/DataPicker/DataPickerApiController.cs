/* Copyright � 2023 Lee Kelleher.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json.Linq;
#if NET472
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
#else
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Extensions;
#endif

namespace Umbraco.Community.Contentment.DataEditors
{
    [PluginController(Constants.Internals.PluginControllerName), IsBackOffice]
    public sealed class DataPickerApiController : UmbracoAuthorizedJsonController
    {
        private readonly IDataTypeService _dataTypeService;
        private readonly ConfigurationEditorUtility _utility;

        internal static readonly Dictionary<Guid, (IDataPickerSource, Dictionary<string, object>)> _lookup = new Dictionary<Guid, (IDataPickerSource, Dictionary<string, object>)>();

        public DataPickerApiController(
            IDataTypeService dataTypeService,
            ConfigurationEditorUtility utility)
        {
            _dataTypeService = dataTypeService;
            _utility = utility;
        }

        [HttpPost]
#if NET472
        public HttpResponseMessage GetItems(Guid dataTypeKey, [FromBody] string[] values)
#else
        public IActionResult GetItems(Guid dataTypeKey, [FromBody] string[] values)
#endif
        {
            if (_lookup.TryGetValue(dataTypeKey, out var cached) == true)
            {
#if NET472
                return Request.CreateResponse(HttpStatusCode.OK, cached.Item1.GetItems(cached.Item2, values).ToDictionary(x => x.Value));
#else
                return Ok(cached.Item1.GetItems(cached.Item2, values).ToDictionary(x => x.Value));
#endif
            }
            else if (_dataTypeService.GetDataType(dataTypeKey) is IDataType dataType &&
                dataType?.EditorAlias.InvariantEquals(DataPickerDataEditor.DataEditorAlias) == true &&
                dataType.Configuration is Dictionary<string, object> dataTypeConfig &&
                dataTypeConfig.TryGetValue(DataPickerConfigurationEditor.DataSource, out var tmp1) == true &&
                tmp1 is JArray array1 &&
                array1.Count > 0 &&
                array1[0] is JObject item1)
            {
                var source1 = _utility.GetConfigurationEditor<IDataPickerSource>(item1.Value<string>("key"));
                if (source1 != null)
                {
#if NET472
                    var config1 = item1?["value"]?.ToObject<Dictionary<string, object>>();
#else
                    var config1 = item1?["value"]?.ToObject<Dictionary<string, object>>()!;
#endif

                    _lookup.TryAdd(dataTypeKey, (source1, config1));

#if NET472
                    return Request.CreateResponse(HttpStatusCode.OK, source1.GetItems(config1, values).ToDictionary(x => x.Value));
#else
                    return Ok(source1.GetItems(config1, values).ToDictionary(x => x.Value));
#endif
                }
            }

#if NET472
            return Request.CreateResponse(HttpStatusCode.NotFound, $"Unable to locate data source for data type: '{dataTypeKey}'");
#else
            return NotFound($"Unable to locate data source for data type: '{dataTypeKey}'");
#endif
        }

        [HttpGet]
#if NET472
        public HttpResponseMessage Search(Guid dataTypeKey, int pageNumber = 1, int pageSize = 12, string query = "")
#else
        public IActionResult Search(Guid dataTypeKey, int pageNumber = 1, int pageSize = 12, string query = "")
#endif
        {
            var totalPages = -1;

            if (_lookup.TryGetValue(dataTypeKey, out var cached) == true)
            {
                var items = cached.Item1.Search(cached.Item2, out totalPages, pageNumber, pageSize, HttpUtility.UrlDecode(query));

#if NET472
                return Request.CreateResponse(HttpStatusCode.OK, new { items, totalPages });
#else
                return Ok(new { items, totalPages });
#endif
            }
            else if (_dataTypeService.GetDataType(dataTypeKey) is IDataType dataType &&
                dataType?.EditorAlias.InvariantEquals(DataPickerDataEditor.DataEditorAlias) == true &&
                dataType.Configuration is Dictionary<string, object> dataTypeConfig &&
                dataTypeConfig.TryGetValue(DataPickerConfigurationEditor.DataSource, out var tmp1) == true &&
                tmp1 is JArray array1 &&
                array1.Count > 0 &&
                array1[0] is JObject item1)
            {
                var source1 = _utility.GetConfigurationEditor<IDataPickerSource>(item1.Value<string>("key"));
                if (source1 != null)
                {
#if NET472
                    var config1 = item1?["value"]?.ToObject<Dictionary<string, object>>();
#else
                    var config1 = item1?["value"]?.ToObject<Dictionary<string, object>>()!;
#endif

                    _lookup.TryAdd(dataTypeKey, (source1, config1));

                    var items = source1?.Search(config1, out totalPages, pageNumber, pageSize, HttpUtility.UrlDecode(query));
#if NET472
                    return Request.CreateResponse(HttpStatusCode.OK, new { items, totalPages });
#else
                    return Ok(new { items, totalPages });
#endif
                }
            }

#if NET472
            return Request.CreateResponse(HttpStatusCode.NotFound, $"Unable to locate data source for data type: '{dataTypeKey}'");
#else
            return NotFound($"Unable to locate data source for data type: '{dataTypeKey}'");
#endif
        }

        // NOTE: The internal cache is cleared from `ContentmentDataTypeNotificationHandler` [LK]
        internal static void ClearCache(Guid dataTypeKey)
        {
            _lookup.Remove(dataTypeKey);
        }
    }
}
