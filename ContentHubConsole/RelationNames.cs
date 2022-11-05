using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole
{
    public static class RelationNames
    {
        public static readonly string RELATION_ASSETTYPE_TOASSET = "AssetTypeToAsset";
        public static readonly string RELATION_ASSETUSAGE_TOASSET = "AssetUsageToAsset";
        public static readonly string RELATION_TAG_TOASSET = "TagToAsset";
        public static readonly string RELATION_COLLECTION_TOASSET = "CollectionToAsset";
        public static readonly string RELATION_BRAND_TOASSET = "PCMBrandToAsset";
        public static readonly string RELATION_PRODUCTFAMILY_TOASSET = "PCMProductFamilyToAsset";
        public static readonly string RELATION_PRODUCTFAMILY_TOMASTERASSET = "PCMProductFamilyToMasterAsset";
        public static readonly string RELATION_PRODUCT_TOASSET = "PCMProductToAsset";
        public static readonly string RELATION_PRODUCT_TOMASTERASSET = "PCMProductToMasterAsset";
        public static readonly string RELATION_CATALOG_TOASSET = "PCMCatalogToAsset";
        public static readonly string RELATION_CATALOG_TOMASTERASSET = "PCMCatalogToMasterAsset";
    }
}
