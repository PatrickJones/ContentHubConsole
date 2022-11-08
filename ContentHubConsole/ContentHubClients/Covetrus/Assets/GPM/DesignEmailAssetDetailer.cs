using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM
{
    public class DesignEmailAssetDetailer : BaseDetailer
    {
        public static readonly string UploadPath = @"C:\Users\ptjhi\Dropbox (Covetrus)\Consumer Creative\GPM\2022\Transactional";

        public DesignEmailAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long gpmStoreFrontBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.GlobalPrescriptionManagement")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long emailId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Contains("Email")).FirstOrDefault().Id.Value;
            long emailUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Contains("Email")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(gpmStoreFrontBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(emailId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(emailUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    SetSlices(asset);
                    SetStoreFrontBanners(asset);
                    SetBusinessToBusiness(asset);
                    SetStockImages(asset);
                    SetProductUsage(asset);
                    SetPromotion(asset);
                    SetEngagementEmail(asset);
                    SetTransactionalEmail(asset);
                    SetShippingEmail(asset);
                    SetReactivationEmail(asset);

                    SetYear(asset);
                    SetMonth(asset);
                    SetWeek(asset);

                    var log = $"New asset {asset.Asset.Id} from path {asset.OriginPath}";
                    Console.WriteLine(log);
                    FileLogger.Log("UpdateAllAssets", log);
                }
                catch (Exception ex)
                {
                    asset.Errors.Add(ex.Message);
                    _failedAssets.Add(asset);
                    continue;
                }
                
            }

            return results.Count;
        }
    }
}
