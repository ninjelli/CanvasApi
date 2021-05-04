﻿using CanvasApi.Client.Modules.Enums;
using System.Collections.Generic;
using TiberHealth.Serializer.Attributes;

namespace CanvasApi.Client.Modules.Models
{
    public interface IModuleItemSequenceOptions
    {
        /// <summary>
        /// The type of asset to find module sequence information for. Use the ModuleItem if it is known(e.g., the user navigated from a module item), since this will avoid ambiguity if the asset appears more than once in the module sequence.
        /// 
        /// Allowed values: <see cref="ModuleItemSequenceAssetTypes"/>
        /// </summary>
        [Multipart("asset_type")] ModuleItemSequenceAssetTypes AssetType { get; set; }
        /// <summary>
        /// The id of the asset (or the url in the case of a Page)
        /// </summary>
        [Multipart("asset_id")] long? AssetId { get; set; }
    }
}