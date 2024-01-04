/* Copyright © 2019 Lee Kelleher.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

angular.module("umbraco").controller("Umbraco.Community.Contentment.DataEditors.ItemPicker.Controller", [
    "$scope",
    "editorService",
    "focusService",
    "localizationService",
    "overlayService",
    function ($scope, editorService, focusService, localizationService, overlayService) {

        //console.log("item-picker.model", $scope.model);

        var defaultConfig = {
            addButtonLabelKey: "general_add",
            allowClear: 0,
            allowDuplicates: 0,
            confirmRemoval: 0,
            defaultIcon: "icon-science",
            defaultValue: [],
            disableSorting: 0,
            displayMode: "list",
            enableFilter: 1,
            enableMultiple: 0,
            items: [],
            maxItems: 0,
            listType: "grid",
            overlayView: "",
            overlayOrderBy: "name",
            overlaySize: "small",
        };
        var config = Object.assign({}, defaultConfig, $scope.model.config);

        var vm = this;

        function init() {

            $scope.model.value = $scope.model.value || config.defaultValue;

            if (Array.isArray($scope.model.value) === false) {
                $scope.model.value = [$scope.model.value];
            }

            if (Number.isInteger(config.maxItems) === false) {
                config.maxItems = Number.parseInt(config.maxItems) || defaultConfig.maxItems;
            }

            if (Array.isArray(config.overlaySize) === true) {
                config.overlaySize = config.overlaySize[0];
            }

            config.confirmRemoval = Object.toBoolean(config.confirmRemoval);
            config.enableMultiple = Object.toBoolean(config.enableMultiple) && config.maxItems !== 1;

            vm.defaultIcon = config.defaultIcon;
            vm.displayMode = config.displayMode || "list";
            vm.allowAdd = config.maxItems === 0 || $scope.model.value.length < config.maxItems;
            //vm.allowEdit = true; // S6 We want to allow editing (or at least opening) Contentment core doesn't have any code to handle this even if forced to 'true'...only looks like it is used for macro-picker.js
            /* S6 Inject base editUrl so property editors can load picked nodes in separate tabs instead of infinite editing mode.
              This probably needs to be suffixed with each vm.item nodeId (either explicitly in their data or dynamically in the umb-preview-node.html?)
              But this might not be the right place to do this? These items are probably the PICKABLE ones...not the ones selected by the user.
            */
            //vm.editUrl = "umbraco/#/content/content/edit/";  // S6 can force the guid portion of a udi as an editUrl slug and U10 will route correctly (like when using an int id)
            vm.allowOpen = true; // S6 TODO Forcing true to try and access infinite editing (TODO Make configuration toggle)
            vm.allowRemove = true; 
            vm.allowSort = Object.toBoolean(config.disableSorting) === false && config.maxItems !== 1;

            vm.addButtonLabelKey = config.addButtonLabelKey || "general_add"; 

            vm.open = open; // S6 Added (keep names conventional in case that matters)
            vm.add = add;
            vm.remove = remove;
            vm.sort = () => {
                $scope.model.value = vm.items.map(item => item.value);
            };

            vm.items = []; // S6 Why is vm.items emptied here after it was mapped to $scope.model.value above?

            if ($scope.model.value.length > 0 && config.items.length > 0) {
                var orphaned = [];
                
                // S6 Set editUrl values for ALL items (config.items or $scope.model.value or vm.items?)
                config.items.forEach(ci => {
                    if (ci.value != undefined && typeof ci.value === "string") {
                        if (ci.value.indexOf("umb://") > -1) {
                            // S6 value is a UDI, extract data portion for editUrl slug
                            ci.editUrl = '/umbraco/#/content/content/edit/' + ci.value.substring(ci.value.lastIndexOf('/') + 1);
                        } else {
                            // S6 assume Guid or int, can use entire value for editUrl slug
                            ci.editUrl = '/umbraco/#/content/content/edit/' + ci.value;
                        }                        
                    }
                });

                console.log('s6contentment config.items: ', config.items);
                console.log('s6contentment $scope.model.value: ', $scope.model.value);

                $scope.model.value.forEach(v => {
                    var item = config.items.find(x => x.value === v);
                    if (item) {                        
                        //if (item.value != undefined && typeof item.value === "string") {
                        //    item.editUrl = 'umbraco/#/content/content/edit/' + item.value.substring(item.value.lastIndexOf('/') + 1); // S6 try injecting editUrl for each item? item only has icon/name/value (udi) ... we need id                        
                        //}                        
                        vm.items.push(Object.assign({}, item));
                    } else {
                        // S6 "item" is null/false here?
                        orphaned.push(v);
                    }
                });

                if (orphaned.length > 0) {
                    $scope.model.value = _.difference($scope.model.value, orphaned); // TODO: Replace Underscore.js dependency. [LK:2020-03-02]

                    if (config.maxItems === 0 || $scope.model.value.length < config.maxItems) {
                        vm.allowAdd = true;
                    }
                }
            }

            if ($scope.umbProperty) {

                vm.propertyActions = [];

                if (Object.toBoolean(config.allowClear) === true) {
                    vm.propertyActions.push({
                        labelKey: "buttons_clearSelection",
                        icon: "trash",
                        method: clear
                    });
                }

                if (vm.propertyActions.length > 0) {
                    $scope.umbProperty.setPropertyActions(vm.propertyActions);
                }
            }
        };

        /* S6 TODO Make "edit" and/or "open" functions to support built-in U10 picker infinite editing
         * U10 core sets "on-open" attribute to a custom openEditor() method:
         * https://github.com/umbraco/Umbraco-CMS/blob/0c595ccc5f88750a8f547e4bbbe58c457864094d/src/Umbraco.Web.UI.Client/src/views/propertyeditors/contentpicker/contentpicker.controller.js#L346
         * Determines type being edited (ie. member/doctype/etc.) and then calls appropriate editorService method
         * Try copy/pasting this entire method, or creating a lite version similar to the "item-picker.js add()" method further below
         * */
        
        function open(item) {
            //console.log('s6open item ', item);

            /* Innards of U10 openEditor, but that operates on a "node" not a custom contentment "item", which only has a UDI and some labels
               Let's start by trying to dup the existing "edit" method from the Contentment configuration-editor.js?
               ...though that is probably a sidebar editor instead of a fullscreen infinite editor
            */

            /* Content "Item" schema:
             * contentment "item" is an object:
                {
                    "description": "",
                    "icon": "",
                    "name": "",
                    "value": "{udi}"
                }
             * */
            var editor = {
                id: item.value, // udi
                submit: function (model) {
                    //console.log('s6 item-picker.js submit model ', model);
                    //var node = entityType === "Member" ? model.memberNode :
                    //    entityType === "Media" ? model.mediaNode :
                    //        model.contentNode;

                    // update the node
                    //item.name = node.name;

                    //if (entityType !== "Member") {
                    //    if (entityType === "Document") {
                    //        item.published = node.hasPublishedVersion;
                    //    }
                    //    entityResource.getUrl(node.id, entityType).then(function (data) {
                    //        item.url = data;
                    //    });
                    //}
                    editorService.close();
                },
                close: function () {
                    editorService.close();
                }
            };

            // Just assume "content" for now
            editorService.contentEditor(editor);
            
            // Native U10 open, based on "node" not contentment "item"
            //var editor = {
            //    id: entityType === "Member" ? item.key : item.id,
            //    submit: function (model) {

            //        var node = entityType === "Member" ? model.memberNode :
            //            entityType === "Media" ? model.mediaNode :
            //                model.contentNode;

            //        // update the node
            //        item.name = node.name;

            //        if (entityType !== "Member") {
            //            if (entityType === "Document") {
            //                item.published = node.hasPublishedVersion;
            //            }
            //            entityResource.getUrl(node.id, entityType).then(function (data) {
            //                item.url = data;
            //            });
            //        }
            //        editorService.close();
            //    },
            //    close: function () {
            //        editorService.close();
            //    }
            //};

            //switch (entityType) {
            //    case "Document":
            //        editorService.contentEditor(editor);
            //        break;
            //    case "Media":
            //        editorService.mediaEditor(editor);
            //        break;
            //    case "Member":
            //        editorService.memberEditor(editor);
            //        break;
            //}
        };

        function add() {

            focusService.rememberFocus();

            var items = Object.toBoolean(config.allowDuplicates)
                ? config.items
                : config.items.filter(x => vm.items.some(y => x.value === y.value) === false);

            editorService.open({
                config: {
                    title: "Choose...",
                    enableFilter: Object.toBoolean(config.enableFilter),
                    enableMultiple: config.enableMultiple,
                    defaultIcon: config.defaultIcon,
                    items: items,
                    listType: config.listType,
                    orderBy: config.overlayOrderBy,
                    maxItems: config.maxItems === 0 ? config.maxItems : config.maxItems - vm.items.length
                },
                view: config.overlayView,
                size: config.overlaySize || "small",
                submit: function (selectedItems) {

                    // NOTE: Edge-case, if the value isn't set and the content is saved, the value becomes an empty string. ¯\_(ツ)_/¯
                    if (typeof $scope.model.value === "string") {
                        $scope.model.value = $scope.model.value.length > 0 ? [$scope.model.value] : config.defaultValue;
                    }

                    selectedItems.forEach(item => {
                        vm.items.push(angular.copy(item)); // TODO: Replace AngularJS dependency. [LK:2020-12-17]
                        $scope.model.value.push(item.value);
                    });

                    if (config.maxItems !== 0 && $scope.model.value.length >= config.maxItems) {
                        vm.allowAdd = false;
                    }

                    editorService.close();

                    setDirty();
                    setFocus();
                },
                close: function () {
                    editorService.close();
                    setFocus();
                }
            });
        };

        function clear() {
            vm.items = [];
            $scope.model.value = [];
            setDirty();
        };

        function remove($index) {
            //console.log('s6 remove item ', $index);
            focusService.rememberFocus();

            if (config.confirmRemoval === true) {
                var keys = ["contentment_removeItemMessage", "general_remove", "general_cancel", "contentment_removeItemButton"];
                localizationService.localizeMany(keys).then(data => {
                    overlayService.open({
                        title: data[1],
                        content: data[0],
                        closeButtonLabel: data[2],
                        submitButtonLabel: data[3],
                        submitButtonStyle: "danger",
                        submit: function () {
                            removeItem($index);
                            overlayService.close();
                        },
                        close: function () {
                            overlayService.close();
                            setFocus();
                        }
                    });
                });
            } else {
                removeItem($index);
            }
        };

        function removeItem($index) {

            vm.items.splice($index, 1);

            $scope.model.value.splice($index, 1);

            if (config.maxItems === 0 || $scope.model.value.length < config.maxItems) {
                vm.allowAdd = true;
            }

            setDirty();
        };

        function setDirty() {
            if ($scope.propertyForm) {
                $scope.propertyForm.$setDirty();
            }
        };

        function setFocus() {
            var lastKnownFocus = focusService.getLastKnownFocus();
            if (lastKnownFocus) {
                lastKnownFocus.focus();
            }
        };

        init();
    }
]);
