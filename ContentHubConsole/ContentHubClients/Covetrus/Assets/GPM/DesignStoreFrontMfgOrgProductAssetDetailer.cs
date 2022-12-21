using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM
{
    public class DesignStoreFrontMfgOrgProductAssetDetailer : BaseDetailer
    {
        private const string GPM_CATALOG_NAME = "GPM Storefront Master Catalog";
        private const string BUSINESS_DOMAIN_NAME = "Business.Domain.GPM.StoreFront";

        public DesignStoreFrontMfgOrgProductAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long gpmStoreFrontBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.GPM.StoreFront")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().Id.Value;
            long imageId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value;
            long imageUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Equals("CV.AssetUsage.Product")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(gpmStoreFrontBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(imageId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(imageUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    await AddTagFromPath(asset);

                    //await AssignToProduct(asset, gpmStoreFrontBusinessDomainId);
                    await AssignToCatalogue(asset, GPM_CATALOG_NAME);

                    SetManufacturerOriginal(asset);
                    //SetHybrisReady(asset);
                    //SetStockImages(asset);
                    //SetProductUsage(asset);
                    //SetOffsite(asset);
                    //SetOnsite(asset);
                    //SetWebpage(asset);
                    SetUsages(asset);
                    //SetSeason(asset);
                    //SetAdvertising(asset);
                    //await AddBrandFromPath(asset);

                    //SetYear(asset);
                    //SetSpecificYear(asset);
                    //SetMonth(asset);
                    //SetWeek(asset);

                    //UpdateAssetType(asset);

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

        public override async Task AssignToProduct(CovetrusAsset asset, long businessDomainId)
        {
            var pathSplit = asset.OriginPath.Split('\\');
            var filename = pathSplit.Last();

            var productNumber = String.Empty;
            if (pathSplit.Any(a => a.Equals("Parent SKUs", StringComparison.InvariantCultureIgnoreCase)))
            {
                var sp = filename.Split('_');
                if (filename.StartsWith("_"))
                {
                    productNumber = sp[1];
                }
                else if (filename.StartsWith("Parent"))
                {
                    var spfn = filename.Split(" ");
                    productNumber = spfn[1].Replace("(", "").Replace(")", "");
                }
                else
                {
                    productNumber = sp[0];
                }
            }

            if (pathSplit.Any(a => a.Equals("Child SKUs", StringComparison.InvariantCultureIgnoreCase)))
            {
                var sp = filename.Split('_');
                if (filename.StartsWith("2-Pack") || filename.StartsWith("12-Pack"))
                {
                    productNumber = sp[1];
                }
                else if (pathSplit.Count() > 6)
                {
                    productNumber = sp.Count() == 1 ? sp[0] : sp[1];
                    var manufactIdentifier = GetMfg(pathSplit[5]);
                    long mfgId = _taxonomyManager.ManufacturerEntities.Where(w => w.Identifier.Equals(manufactIdentifier)).FirstOrDefault().Id.Value;
                    asset.SetChildToOneParentRelation(mfgId, CovetrusRelationNames.RELATION_MANUFACTURER_TOASSET);
                }
                else
                {
                    productNumber = sp[1];
                }

                await _productManager.SetProductAsChildByNumber(productNumber, businessDomainId);
            }

            var products = await _productManager.GetProductByNumber(productNumber, businessDomainId);

            if (products.Count > 0)
            {
                foreach (var p in products)
                {
                    asset.AddChildToManyParentsRelation(p.Id.Value, RelationNames.RELATION_PRODUCT_TOASSET);
                }
            }
        }

        private void SetManufacturerOriginal(CovetrusAsset asset)
        {
            asset.MarkAsManufacturerOriginal();
        }

        private void SetHybrisReady(CovetrusAsset asset)
        {
            var pathSplit = asset.OriginPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("Hybris Ready")))
            {
                asset.MarkAsHybrisReady();
            }
        }

        public override string RemoveLocalPathPart(string originPath)
        {
            var pathSplit = Program.IsVirtualMachine ? originPath.Split('\\').Skip(1).ToArray() : originPath.Split('\\').Skip(3).ToArray();
            var result = new StringBuilder();

            for (int i = 0; i < pathSplit.Length; i++)
            {
                result.Append($"\\{pathSplit[i]}");
            }

            return result.ToString();
        }

        internal void SetUsages(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Compounds", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Compounds"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }

            var pathStrings = new List<string>();
            var orgSpilt = asset.OriginPath.Split("\\").ToList();

            foreach (var s in orgSpilt)
            {
                var strSplit = s.Split(' ');
                pathStrings.AddRange(strSplit);
            }

            foreach (var ps in pathStrings)
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.EndsWith(ps, StringComparison.CurrentCultureIgnoreCase)
                    || w.Identifier.Contains(ps, StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private async Task AddTagFromPath(CovetrusAsset asset)
        {
            if (asset.OriginPath.Split('\\').Any(a => a.Equals("F22", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('-', ' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Lifestyle", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = Program.IsVirtualMachine ? asset.OriginPath.Split('\\').Skip(6).Take(1).FirstOrDefault() : asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('_', ' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("SmartPaks & Supporting", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('_', ' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }
        }

        private async Task AddBrandFromPath(CovetrusAsset asset)
        {
            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Brands", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddBrandValue(tag);
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_BRAND_TOASSET);
                }
            }
        }

        private void SetSpecificYear(CovetrusAsset asset)
        {
            long yearId = 0;

            var pathSplit = asset.OriginPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("2021_ColiCare")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2021")).FirstOrDefault();
                yearId = tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("F22")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2022")).FirstOrDefault();
                yearId = tax.Id.Value;
            }

            if (yearId != 0)
            {
                asset.SetChildToOneParentRelation(yearId, CovetrusRelationNames.RELATION_YEAR_TOASSET);
            }
        }

        private void UpdateAssetType(CovetrusAsset asset)
        {
            var pathDirectoryCount = asset.OriginPath.Split('\\').Count();

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Illustrations", StringComparison.InvariantCultureIgnoreCase)))
            {
                var assetId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Illustration")).FirstOrDefault().Id.Value;
                asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Lifestyle", StringComparison.InvariantCultureIgnoreCase)))
            {
                var assetId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Lifestyle")).FirstOrDefault().Id.Value;
                asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Labels", StringComparison.InvariantCultureIgnoreCase)))
            {
                var typeCheck = asset.OriginPath.Split('\\').Skip(7).Take(1).FirstOrDefault();

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Equals("labels", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Label")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Contains("font", StringComparison.InvariantCultureIgnoreCase))
                && asset.OriginPath.Split('\\').Any(a => a.Contains("template", StringComparison.InvariantCultureIgnoreCase)))
            {
                var typeCheck = asset.OriginPath.Split('\\').Skip(12).Take(1).FirstOrDefault();

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Equals("banners", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Banner")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Equals("email", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Email")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Equals("social", StringComparison.InvariantCultureIgnoreCase) || typeCheck.Equals("paidsocial", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.SocialMedia")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                }
            }
        }

        private string GetMfg(string name)
        {
            var mfg = name switch
            {
                _ when name.Equals("Arenus", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Arenus",
                _ when name.Equals("Adequan", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Adequan",
                _ when name.Equals("Akorn", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Akorn.90821",
                _ when name.Equals("AmerisourceBergen", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.AmerisourceBergen",
                _ when name.Equals("Bayer", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Bayer.00108",
                _ when name.StartsWith("BIAH", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.BIAH",
                _ when name.Equals("Bimeda", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Bimeda.16030",
                _ when name.Equals("BIV", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.BIV",
                _ when name.Equals("BI", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.BI",
                _ when name.Equals("Blue Naturals", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.BlueNaturals",
                _ when name.Equals("Boehringer Ingelheim", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.BoehringerIngelheim.00157",
                _ when name.Equals("Centaur", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Centaur",
                _ when name.Equals("Ceva", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Ceva.00205",
                _ when name.StartsWith("Covetrus", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.CovetrusPrivateLabel",
                _ when name.Equals("Dechra", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.DECHRA",
                _ when name.Equals("Dermoscent", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Dermoscent",
                _ when name.Equals("EquiMedic", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.EquiMedic",
                _ when name.Equals("Elanco", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Elanco.96289",
                _ when name.Equals("Equithrive", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Equithrive",
                _ when name.Equals("Flair", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Flair",
                _ when name.Equals("Galliprant", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Galliprant",
                _ when name.Equals("Glandex", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Glandex",
                _ when name.Equals("GPM Products", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.GPMProducts",
                _ when name.Equals("Greenies", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Greenies",
                _ when name.StartsWith("Henry", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.HenryScheinAnimalHealth",
                _ when name.Equals("Hills", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Hills",
                _ when name.Equals("Hill's", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Hills",
                _ when name.Equals("Honest Kitchen", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.HonestKitchen",
                _ when name.StartsWith("Kentucky", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.KentuckyPerformanceProducts",
                _ when name.StartsWith("Kindred Biosciences", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.KindredBiosciences",
                _ when name.Equals("Kinetic Vet", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.KineticVet",
                _ when name.Equals("KRUUSE", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.KRUUSE.20415",
                _ when name.Equals("Lloyd Labs", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.LloydLabs",
                _ when name.Equals("Luitpold", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Luitpold",
                _ when name.Equals("Lintbells", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Lintbells",
                _ when name.Equals("MediNatura", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.MediNatura",
                _ when name.Equals("Merial", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Merial.94986",
                _ when name.Equals("Merck", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Merck.00195",
                _ when name.StartsWith("Mizner", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.MiznerBioscience",
                _ when name.Equals("Neogen", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Neogen.01165",
                _ when name.Equals("Nutramax", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Nutramax.55031",
                _ when name.Equals("Nutro", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Nutro.01194",
                _ when name.Equals("Parnell", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Parnell",
                _ when name.Equals("Pet Essentials", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.PetEssentials",
                _ when name.StartsWith("PRN", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.PRNPharmacal.01431",
                _ when name.Equals("Purina", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Purina.13492",
                _ when name.Equals("Putney", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Putney.06646",
                _ when name.Equals("Rayne", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Rayne",
                _ when name.Equals("Reymard", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Reymard",
                _ when name.Equals("Royal Canin", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.RoyalCanin.82051",
                _ when name.Equals("Road Runner Compounds", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.RoadRunnerCompounds",
                _ when name.Equals("SmartPak", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.SmartPak",
                _ when name.Equals("Smart Pak", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.SmartPak",
                _ when name.Equals("Vedco", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Vedco.15022",
                _ when name.Equals("Vetone", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.VetOne",
                _ when name.Equals("Vet One", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.VetOne",
                _ when name.Equals("Vet Kem", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.VetKem",
                _ when name.Equals("Vetericyn", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Vetericyn",
                _ when name.Equals("Vetoquinol", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Vetoquinol.00507",
                _ when name.Equals("Vetoquinol", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Vetoquinol.00507",
                _ when name.Equals("Vetri-Science", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.VetriScience",
                _ when name.Equals("Vetri Science", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.VetriScience",
                _ when name.Equals("Virbac", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Virbac.00202",
                _ when name.Equals("Visbiome", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Visbiome",
                _ when name.Equals("VPL", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.VPL.00281",
                _ when name.Equals("Zoetis", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Zoetis.07823",
                _ when name.Equals("Zymax", StringComparison.InvariantCultureIgnoreCase) => "CV.Manufacturer.Zymax",
                _ => String.Empty
            };

            return mfg;
        }
    }
}