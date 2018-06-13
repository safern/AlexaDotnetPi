using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SmartHomeApplet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AlexaSkill.Models
{
    public class StateMachine
    {
        private const string _cloudTableName = "StateMachineCache";
        private const int TIMESTAMP_TOLERANCE_SECONDS = 60;

        private static CloudStorageAccount _storageAccount;
        private static CloudTableClient _tableClient;
        private static CloudTable _table;

        private CloudTable Table
        {
            get
            {
                if (_table == null)
                {
                    _storageAccount = CloudStorageAccount.Parse(Startup.Configuration["azure:connectionstring"]);
                    _tableClient = _storageAccount.CreateCloudTableClient();
                    _table = _tableClient.GetTableReference(_cloudTableName);
                }
                return _table;
            }
        }

        public StateMachine()
        {
            bool tableCreated = Table.CreateIfNotExistsAsync().Result;
            Debug.WriteLine($"Table created result: {tableCreated}");
        }

        public StateEntity SetSensorState(string userId, string controller, string sensor, StateValues state)
        {
            StateEntity newEntity = new StateEntity(userId, controller, sensor);
            newEntity.State = state.ToString();

            TableOperation insertOperation = TableOperation.InsertOrReplace(newEntity);
            return (StateEntity)Table.ExecuteAsync(insertOperation).Result.Result;
        }

        public bool TryGetSensorState(string userId, string controller, string sensor, out StateEntity result)
        {
            TableOperation retriveOperation = TableOperation.Retrieve<StateEntity>(userId, StateEntity.GenerateRowKey(controller, sensor));
            TableResult retrievedResult = Table.ExecuteAsync(retriveOperation).Result;
            if (retrievedResult.Result != null)
            {
                result = (StateEntity)retrievedResult.Result;
                return true;
            }
            else
            {
                result = null;
                Debug.WriteLine($"Unable to retrieve user {userId} controller {controller} sensor {sensor}");
                return false;
            }
        }

        public bool IsEntityUpToDate(StateEntity entity)
        {
            var diff = DateTime.UtcNow - entity.Timestamp.UtcDateTime;
            Debug.WriteLine("State value was updated {0:0.00} seconds ago.", diff.TotalSeconds);
            return (Math.Abs((decimal)diff.TotalSeconds) <= TIMESTAMP_TOLERANCE_SECONDS);
        }
    }

    public class StateEntity : TableEntity
    {
        public StateEntity() { }
        public StateEntity(string user, string controller, string sensor)
        {
            this.PartitionKey = user;
            this.RowKey = GenerateRowKey(controller, sensor);
        }

        public static string GenerateRowKey(string controller, string sensor)
        {
            return string.Concat(controller, "@", sensor);
        }

        public string State { get; set; }
    }

    public enum StateValues
    {
        Off,
        On
    }
}
