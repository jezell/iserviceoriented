using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using System.Data.SqlClient;

using System.Runtime.Serialization;


namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Provides data access functionality for SqlSubscriptionPersistenceService
    /// </summary>
    public class SqlSubscriptionDB : ISubscriptionDB
    {
        public SqlSubscriptionDB(string connectionString, Type[] knownDispatcherTypes, Type[] knownListenerTypes, Type[] knownFilterTypes)
        {
            _connectionString = connectionString;
            _dispatcherSerializer = new DataContractSerializer(typeof(Dispatcher), knownDispatcherTypes);
            _listenerSerializer = new DataContractSerializer(typeof(Listener), knownListenerTypes);
            _filterSerializer = new DataContractSerializer(typeof(MessageFilter), knownFilterTypes);
        }

        DataContractSerializer _dispatcherSerializer;
        DataContractSerializer _listenerSerializer;
        DataContractSerializer _filterSerializer;

        Listener getListenerFromPersistenceData(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            ms.Position = 0;
            return (Listener)_listenerSerializer.ReadObject(ms);
        }

        byte[] getListenerPersistenceData(Listener obj)
        {
            MemoryStream ms = new MemoryStream();
            _listenerSerializer.WriteObject(ms, obj);
            return ms.ToArray();
        }

        MessageFilter getFilterFromPersistenceData(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            ms.Position = 0;
            return (MessageFilter)_filterSerializer.ReadObject(ms);
        }

        byte[] getFilterPersistenceData(MessageFilter obj)
        {
            MemoryStream ms = new MemoryStream();
            _filterSerializer.WriteObject(ms, obj);
            return ms.ToArray();
        }


        Dispatcher getDispatcherFromPersistenceData(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            ms.Position = 0;
            return (Dispatcher)_dispatcherSerializer.ReadObject(ms);
        }

        byte[] getDispatcherPersistenceData(Dispatcher obj)
        {
            MemoryStream ms = new MemoryStream();
            _dispatcherSerializer.WriteObject(ms, obj);
            return ms.ToArray();
        }
        
        string _connectionString;

        SqlConnection getConnection()
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        static void executeScript(SqlConnection connection, StreamReader scriptReader)
        {
            using (SqlCommand create = new SqlCommand())
            {
                create.Connection = connection;
                StringBuilder commandBuilder = new StringBuilder();

                string line = null;
                while ((line = scriptReader.ReadLine()) != null)
                {
                    if (String.Compare(line.Trim(), "go", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        create.CommandText = commandBuilder.ToString();
                        create.ExecuteNonQuery();
                        commandBuilder.Length = 0;
                    }
                    else
                    {
                        commandBuilder.AppendLine(line);
                    }
                }

                // run last
                create.CommandText = commandBuilder.ToString();
                create.ExecuteNonQuery();
            
            }
        }

        public static void CreateDB(string server, string dbName)
        {
            using (SqlConnection connection = new SqlConnection("Data Source=" + server + "; Integrated Security=SSPI;"))
            {
                connection.Open();
                using (SqlCommand createDb = new SqlCommand("CREATE DATABASE [" + dbName + "]", connection))
                {
                    createDb.ExecuteNonQuery();
                }

                using (SqlCommand useDb = new SqlCommand("USE [" + dbName + "]", connection))
                {
                    useDb.ExecuteNonQuery();
                }

                using (StreamReader sr = new StreamReader(typeof(SqlSubscriptionDB).Assembly.GetManifestResourceStream("IServiceOriented.ServiceBus.CreateSqlSubscriptionPersistenceServiceDb.sql")))
                {
                    executeScript(connection, sr);
                }
                
            }            
        }

        public static void DropDB(string server, string dbName)
        {
            using (SqlConnection connection = new SqlConnection("Data Source=" + server + "; Integrated Security=SSPI;"))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("DROP DATABASE [" + dbName + "]", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }


        SubscriptionEndpoint getSubscription(IDataReader dr)
        {
            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(
                (Guid)dr["id"],
                dr["name"] as string,
                dr["configuration_name"] as string,
                dr["address"] as string,
                Type.GetType(dr["contract_type"] as string),
                getDispatcherFromPersistenceData((byte[])dr["dispatcher_data"]),
                getFilterFromPersistenceData((byte[])dr["filter_data"]))
            ;

            if (endpoint.ContractType == null)
            {
                throw new InvalidOperationException("Contract type could not be loaded");
            }

            return endpoint;
        }

        ListenerEndpoint getListener(IDataReader dr)
        {
            ListenerEndpoint endpoint = new ListenerEndpoint(
                (Guid)dr["id"],
                dr["name"] as string,
                dr["configuration_name"] as string,
                dr["address"] as string,
                Type.GetType(dr["contract_type"] as string),
                getListenerFromPersistenceData((byte[])dr["listener_data"]));

            if (endpoint.ContractType == null)
            {
                throw new InvalidOperationException("Contract type could not be loaded");
            }

            return endpoint;
        }

        public IEnumerable<ListenerEndpoint> LoadListenerEndpoints()
        {
            List<ListenerEndpoint> listenerEndpoints = new List<ListenerEndpoint>();
            using (SqlConnection connection = getConnection())
            {

                using (SqlCommand command = new SqlCommand("sp_listener_list", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            ListenerEndpoint listener = getListener(dataReader);
                            listenerEndpoints.Add(listener);
                        }
                    }
                }
            }
            return listenerEndpoints;
        }

        public IEnumerable<SubscriptionEndpoint> LoadSubscriptionEndpoints()
        {
            List<SubscriptionEndpoint> endpoints = new List<SubscriptionEndpoint>();
            using (SqlConnection connection = getConnection())
            {
                using (SqlCommand command = new SqlCommand("sp_subscription_list", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            SubscriptionEndpoint subscription = getSubscription(dataReader);
                            endpoints.Add(subscription);
                        }
                    }
                }
            }
            return endpoints;
        }

        public void CreateListener(ListenerEndpoint endpoint)
        {
            using (SqlConnection connection = getConnection())
            {
                using (SqlCommand command = new SqlCommand("sp_listener_create", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@id", endpoint.Id);
                    command.Parameters.AddWithValue("@address", endpoint.Address);
                    command.Parameters.AddWithValue("@configuration_name", endpoint.ConfigurationName);
                    command.Parameters.AddWithValue("@contract_type", endpoint.ContractTypeName);
                    command.Parameters.AddWithValue("@name", endpoint.Name);
                    command.Parameters.AddWithValue("@listener_data", getListenerPersistenceData(endpoint.Listener));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void CreateSubscription(SubscriptionEndpoint subscription)
        {
            using (SqlConnection connection = getConnection())
            {
                using (SqlCommand command = new SqlCommand("sp_subscription_create", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@id", subscription.Id);
                    command.Parameters.AddWithValue("@filter_data", getFilterPersistenceData(subscription.Filter));
                    command.Parameters.AddWithValue("@address", subscription.Address);
                    command.Parameters.AddWithValue("@configuration_name", subscription.ConfigurationName);
                    command.Parameters.AddWithValue("@contract_type", subscription.ContractTypeName);
                    command.Parameters.AddWithValue("@name", subscription.Name);
                    command.Parameters.AddWithValue("@dispatcher_data", getDispatcherPersistenceData(subscription.Dispatcher));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteListener(Guid endpointId)
        {
            using (SqlConnection connection = getConnection())
            {
                using (SqlCommand command = new SqlCommand("sp_listener_delete", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@id", endpointId);
                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new ListenerNotFoundException();
                    }
                }
            }
        }

        public void DeleteSubscription(Guid id)
        {
            using (SqlConnection connection = getConnection())
            {
                using (SqlCommand command = new SqlCommand("sp_subscription_delete", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@id", id);
                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new SubscriptionNotFoundException();
                    }
                }
            }
        }
    }
}
