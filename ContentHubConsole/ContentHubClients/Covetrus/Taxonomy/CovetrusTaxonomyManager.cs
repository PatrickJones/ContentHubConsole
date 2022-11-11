using ContentHubConsole.Taxonomies;
using Nito.AsyncEx;
using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Models.Audit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Taxonomy
{
    public class CovetrusTaxonomyManager : TaxonomyManager
    {
        public readonly string BUSINESS_DOMAIN = "Business.Domain";
        public readonly string CV_MIGRATION_ORIGIN = "CV.Migration.Origin";
        public readonly string CV_MANUFACTURER = "CV.Manufacturer";
        public readonly string CV_MONTH = "CV.Month";
        public readonly string CV_PRACTICE = "CV.Practice";
        public readonly string CV_PRACTICETYPE = "CV.PracticeType";
        public readonly string CV_WEBPAGE = "CV.WebPage";
        public readonly string CV_WEEK = "CV.Week";
        public readonly string CV_YEAR = "CV.Year";
        public readonly string CV_ASSET_USAGE = "CV.AssetUsage";
        public readonly string CV_SEASON = "CV.Season";

        public ICollection<IEntity> BusinessDomainEntities = new List<IEntity>();
        public ICollection<IEntity> MigrationOriginEntities = new List<IEntity>();
        public ICollection<IEntity> ManufacturerEntities = new List<IEntity>();
        public ICollection<IEntity> YearEntities = new List<IEntity>();
        public ICollection<IEntity> MonthEntities = new List<IEntity>();
        public ICollection<IEntity> WeekEntities = new List<IEntity>();
        public ICollection<IEntity> PracticeEntities = new List<IEntity>();
        public ICollection<IEntity> PracticeTypeEntities = new List<IEntity>();
        public ICollection<IEntity> WebPageEntities = new List<IEntity>();
        public ICollection<IEntity> AssetUsageEntities = new List<IEntity>();
        public ICollection<IEntity> SeasonEntities = new List<IEntity>();

        public CovetrusTaxonomyManager(IWebMClient webMClient) : base(webMClient)
        {
        }

        public async Task LoadAllTaxonomies()
        {
            await LoadAllDefaultTaxonomies();

            var tasks = new List<Task>();

            tasks.Add(LoadBusinessDomains());
            tasks.Add(LoadMigrationOrigins());
            tasks.Add(LoadManufacturers());
            tasks.Add(LoadWeeks());
            tasks.Add(LoadYears());
            tasks.Add(LoadMonths());
            tasks.Add(LoadWebPages());
            tasks.Add(LoadPractice());
            tasks.Add(LoadPracticeType());
            tasks.Add(LoadAssetUsages());
            tasks.Add(LoadSeasons());

            await tasks.WhenAll();
        }

        public async Task<long> AddManufacturerValue(string manufacturerValue)
        {
            var tagLower = manufacturerValue;
            var tagValueTrimmed = CondenseValue(tagLower, false);
            try
            {
                var query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.DefinitionName == CV_MANUFACTURER && e.Property("TaxonomyName") == tagLower
                  select e).Skip(0).Take(3000));
                var mq = await _webMClient.Querying.QueryAsync(query);

                if (mq.Items.Any())
                {
                    var tags = mq.Items.ToList();
                    return tags.Select(s => s.Id.Value).FirstOrDefault();
                }

                if (!String.IsNullOrEmpty(tagValueTrimmed))
                {
                    var tagEntity = await _webMClient.EntityFactory.CreateAsync(CV_MANUFACTURER);
                    tagEntity.Identifier = $"{CV_MANUFACTURER}.{tagValueTrimmed}";
                    tagEntity.SetPropertyValue("TaxonomyName", tagLower);
                    tagEntity.SetPropertyValue("TaxonomySynonyms", CultureInfo.CurrentCulture, tagLower);
                    tagEntity.SetPropertyValue("TaxonomyDescription", CultureInfo.CurrentCulture, tagLower);
                    tagEntity.SetPropertyValue("TaxonomyLabel", CultureInfo.CurrentCulture, tagLower);

                    return await _webMClient.Entities.SaveAsync(tagEntity);
                }
            }
            catch (Exception ex)
            {
                var error = $"Error adding new CV_MANUFACTURER: {tagLower}";
                Console.WriteLine(error);
                FileLogger.Log("AddManufacturerValue", error);
            }

            return 0;
        }

        private async Task LoadSeasons(int skip = 0, int take = 100)
        {
            if (!MigrationOriginEntities.Any())
            {
                var seasons = await _webMClient.Entities.GetByDefinitionAsync(CV_SEASON, null, skip, take);
                SeasonEntities = seasons.Items.ToList();
            }
        }

        private async Task LoadBusinessDomains(int skip = 0, int take = 100)
        {
            if (!BusinessDomainEntities.Any())
            {
                var businessDomains = await _webMClient.Entities.GetByDefinitionAsync(BUSINESS_DOMAIN, null, skip, take);
                BusinessDomainEntities = businessDomains.Items.ToList();
            }
        }

        private async Task LoadMigrationOrigins(int skip = 0, int take = 100)
        {
            if (!MigrationOriginEntities.Any())
            {
                var migrations = await _webMClient.Entities.GetByDefinitionAsync(CV_MIGRATION_ORIGIN, null, skip, take);
                MigrationOriginEntities = migrations.Items.ToList();
            }
        }

        private async Task LoadManufacturers(int skip = 0, int take = 100)
        {
            if (!ManufacturerEntities.Any())
            {
                var maufacturers = await _webMClient.Entities.GetByDefinitionAsync(CV_MANUFACTURER, null, skip, take);
                ManufacturerEntities = maufacturers.Items.ToList();
            }
        }

        private async Task LoadYears(int skip = 0, int take = 100)
        {
            if (!YearEntities.Any())
            {
                var years = await _webMClient.Entities.GetByDefinitionAsync(CV_YEAR, null, skip, take);
                YearEntities = years.Items.ToList();
            }
        }

        private async Task LoadMonths(int skip = 0, int take = 100)
        {
            if (!MonthEntities.Any())
            {
                var months = await _webMClient.Entities.GetByDefinitionAsync(CV_MONTH, null, skip, take);
                MonthEntities = months.Items.ToList();
            }
        }

        private async Task LoadWeeks(int skip = 0, int take = 100)
        {
            if (!WeekEntities.Any())
            {
                var weeks = await _webMClient.Entities.GetByDefinitionAsync(CV_WEEK, null, skip, take);
                WeekEntities = weeks.Items.ToList();
            }
        }

        private async Task LoadPractice(int skip = 0, int take = 100)
        {
            if (!PracticeEntities.Any())
            {
                var practices = await _webMClient.Entities.GetByDefinitionAsync(CV_PRACTICE, null, skip, take);
                PracticeEntities = practices.Items.ToList();
            }
        }

        private async Task LoadPracticeType(int skip = 0, int take = 100)
        {
            if (!PracticeTypeEntities.Any())
            {
                var practiceTypes = await _webMClient.Entities.GetByDefinitionAsync(CV_PRACTICETYPE, null, skip, take);
                PracticeTypeEntities = practiceTypes.Items.ToList();
            }
        }

        private async Task LoadWebPages(int skip = 0, int take = 100)
        {
            if (!WebPageEntities.Any())
            {
                var webpages = await _webMClient.Entities.GetByDefinitionAsync(CV_WEBPAGE, null, skip, take);
                WebPageEntities = webpages.Items.ToList();
            }
        }
        private async Task LoadAssetUsages(int skip = 0, int take = 100)
        {
            if (!AssetUsageEntities.Any())
            {
                var usages = await _webMClient.Entities.GetByDefinitionAsync(CV_ASSET_USAGE, null, skip, take);
                AssetUsageEntities = usages.Items.ToList();
            }
        }
    }
}
