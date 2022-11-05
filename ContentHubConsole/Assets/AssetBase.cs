using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tavis.UriTemplates;
using static Stylelabs.M.Sdk.Constants;
using static Stylelabs.M.Sdk.Errors;

namespace ContentHubConsole.Assets
{
    public class AssetBase
    {
        private readonly IWebMClient _webMClient;
        private readonly long _assetId;
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

        public IEntity Asset = null;

        public const string PROPERTY_DESCRIPTION = "Description";
        public readonly string OriginPath = String.Empty;

        public List<string> Errors = new List<string>();

        public AssetBase(IWebMClient webMClient, FileUploadResponse fileUploadResponse)
        {
            _webMClient = webMClient;
            _assetId = fileUploadResponse.SuccessfulUpload ? fileUploadResponse.AssetId : 0;
            OriginPath = fileUploadResponse.LocalPath;
        }

        public AssetBase(IWebMClient webMClient, long assetId)
        {
            _webMClient = webMClient;
            _assetId = assetId;
        }

        public async Task LoadAssetMembers()
        {
            if (Asset == null)
            {
                Asset = await _webMClient.Entities.GetAsync(_assetId);
                await Asset.LoadMembersAsync(PropertyLoadOption.All, RelationLoadOption.All);
            }
        }

        public async Task<long> SaveAsset()
        {
            long result = 0;
            try
            {
                if (Asset != null)
                {
                    result = await _webMClient.Entities.SaveAsync(Asset);
                }

                return result;
            }
            catch (Exception ex)
            {
                var e = $"SaveAsset: {ex.Message}";
                Console.WriteLine(e);
                FileLogger.Log("SaveAsset", e);
                Errors.Add(e);
                return result;
            }
        }

        public async Task UpdateDescription(string description)
        {
            try
            {
                var desc = await Asset.GetPropertyValueAsync<string>(PROPERTY_DESCRIPTION, _culture);
                var stringBuilder = desc == null ? new StringBuilder() : new StringBuilder(desc);
                stringBuilder.AppendLine(description);

                Asset.SetPropertyValue(PROPERTY_DESCRIPTION, _culture, stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                var e = $"UpdateDescription: {ex.Message}";
                Console.WriteLine(e);
                FileLogger.Log("UpdateDescription", e);
                Errors.Add(e);
            }
        }

        public void UpdateProperties(List<KeyValuePair<string, string>> properties)
        {
            try
            {
                foreach (var kv in properties)
                {
                    Asset.SetPropertyValue(kv.Key, kv.Value);
                }
            }
            catch (Exception ex)
            {
                var e = $"UpdateProperties: {ex.Message}";
                Console.WriteLine(e);
                FileLogger.Log("UpdateProperties", e);
                Errors.Add(e);
            }
        }

        public void AddChildToManyParentsRelation(long relationId, string relationName)
        {
            try
            {
                var relation = Asset.GetRelation<IChildToManyParentsRelation>(relationName);
                relation.Parents.Add(relationId);
            }
            catch (Exception ex)
            {
                var e = $"AddChildToManyParentsRelation: {ex.Message}";
                Console.WriteLine(e);
                FileLogger.Log("AddChildToManyParentsRelation", e);
                Errors.Add(e);
            }
        }

        public void SetChildToOneParentRelation(long relationId, string relationName)
        {
            try
            {
                var relation = Asset.GetRelation<IChildToOneParentRelation>(relationName);
                relation.SetId(relationId);
            }
            catch (Exception ex)
            {
                var e = $"SetChildToOneParentRelation: {ex.Message}";
                FileLogger.Log("SetChildToOneParentRelation", e);
                Console.WriteLine(e);
                Errors.Add(e);
            }
        }

        public void AddParentToOneChildRelation(long relationId, string relationName)
        {
            try
            {
                var relation = Asset.GetRelation<IParentToOneChildRelation>(relationName);
                relation.SetId(relationId);
            }
            catch (Exception ex)
            {
                var e = $"AddParentToOneChildRelation: {ex.Message}";
                Console.WriteLine(e);
                FileLogger.Log("AddParentToOneChildRelation", e);
                Errors.Add(e);
            }
        }

        public void AddParentToManyChildrenRelation(long relationId, string relationName)
        {
            try
            {
                var relation = Asset.GetRelation<IParentToManyChildrenRelation>(relationName);
                relation.Children.Add(relationId);
            }
            catch (Exception ex)
            {
                var e = $"AddParentToManyChildrenRelation: {ex.Message}";
                Console.WriteLine(e);
                FileLogger.Log("AddParentToManyChildrenRelation", e);
                Errors.Add(e);
            }
        }
    }
}
