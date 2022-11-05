using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.Entities
{
    public class EntityManager
    {
        private readonly IWebMClient _webMClient;

        public EntityManager(IWebMClient webMClient)
        {
            _webMClient = webMClient;
        }

        public async Task<long> UpdateEntityProperties(long entityId, List<KeyValuePair<string, string>> properties)
        {
            try
            {
                var entity = await _webMClient.Entities.GetAsync(entityId);
                await entity.LoadPropertiesAsync(PropertyLoadOption.All);

                foreach (var kv in properties)
                {
                    entity.SetPropertyValue(kv.Key, kv.Value);
                }

                return await _webMClient.Entities.SaveAsync(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                FileLogger.Log("UpdateEntityProperties", ex.Message);

                return 0;
            }
        }

        public async Task<long> AddEntityRelation(long entityId, string relationName, long relationId)
        {
            try
            {
                var entity = await _webMClient.Entities.GetAsync(entityId);
                await entity.LoadRelationsAsync(RelationLoadOption.All);

                var relation = entity.GetRelation(relationName);
                var currentIds = relation.GetIds();
                currentIds.Add(relationId);
                relation.SetIds(currentIds);

                return await _webMClient.Entities.SaveAsync(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                FileLogger.Log("AddEntityRelation", ex.Message);
                return 0;
            }
        }
    }
}
