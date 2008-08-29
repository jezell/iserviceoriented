using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;

using System.IO;

using System.Data;
using System.Data.SqlClient;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;


namespace IServiceOriented.ServiceBus
{
    public class SqlSubscriptionDB
    {
        public SqlSubscriptionDB(string connectionString)
        {
            _connectionString = connectionString;
        }
        string _connectionString;

        SqlConnection getConnection()
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public static void CreateDB(string server, string dbName)
        {
            using (SqlConnection connection = new SqlConnection("Data Source=" + server + "; Integrated Security=SSPI;"))
            {
                Server serverObj = new Server(new ServerConnection(connection));

                using (StreamReader sr = new StreamReader(typeof(SqlSubscriptionDB).Assembly.GetManifestResourceStream("IServiceOriented.ServiceBus.CreateSqlSubscriptionPersistenceServiceDb.sql")))
                {
                    string createScript = sr.ReadToEnd();
                    serverObj.ConnectionContext.ExecuteNonQuery("CREATE DATABASE [" + dbName + "]\r\nGO\r\nUSE ["+dbName+"]");
                    serverObj.ConnectionContext.ExecuteNonQuery(createScript);
                    serverObj.ConnectionContext.ExecuteNonQuery("USE [master]");
                    serverObj.ConnectionContext.Cancel();
                }
            }
        }

        public static void DropDB(string server, string dbName)
        {
            using (SqlConnection connection = new SqlConnection("Data Source=" + server + "; Integrated Security=SSPI;"))
            {
                Server serverObj = new Server(new ServerConnection(connection));
                serverObj.ConnectionContext.ExecuteNonQuery("USE [master]\r\nGO\r\nDROP DATABASE [" + dbName + "]");
                serverObj.ConnectionContext.Cancel();
            }
        }


        static SubscriptionEndpoint getSubscription(IDataReader dr)
        {
            MessageFilter filter = null;

            string filterData = dr["filter_data"] as string;
            string filterTypeString = dr["filter_type"] as string;

            if (filterTypeString != null)
            {
                Type filterType = Type.GetType(filterTypeString);
                if (filterType == null)
                {
                    throw new InvalidOperationException("Filter type could not be loaded");
                }

                filter = (MessageFilter)Activator.CreateInstance(filterType);
                filter.InitFromString(filterData);
            }

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(
                (Guid)dr["id"],
                dr["name"] as string,
                dr["configuration_name"] as string,
                dr["address"] as string,
                Type.GetType(dr["contract_type"] as string),
                Type.GetType(dr["dispatcher_type"] as string),
                filter)
            ;

            if (endpoint.ContractType == null)
            {
                throw new InvalidOperationException("Contract type could not be loaded");
            }

            return endpoint;
        }

        static ListenerEndpoint getListener(IDataReader dr)
        {
            ListenerEndpoint endpoint = new ListenerEndpoint(            
                (Guid)dr["id"],
                dr["name"] as string,
                dr["configuration_name"] as string,
                dr["address"] as string,
                Type.GetType(dr["contract_type"] as string),
                Type.GetType(dr["listener_type"] as string)
            );

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
                    command.Parameters.AddWithValue("@listener_type", endpoint.ListenerTypeName);
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
                    command.Parameters.AddWithValue("@filter_type", subscription.Filter == null ? DBNull.Value : (object)subscription.Filter.GetType().AssemblyQualifiedName);
                    command.Parameters.AddWithValue("@filter_data", subscription.Filter == null ? DBNull.Value :  (object)subscription.Filter.CreateInitString());
                    command.Parameters.AddWithValue("@address", subscription.Address);
                    command.Parameters.AddWithValue("@configuration_name", subscription.ConfigurationName);
                    command.Parameters.AddWithValue("@contract_type", subscription.ContractTypeName);
                    command.Parameters.AddWithValue("@name", subscription.Name);
                    command.Parameters.AddWithValue("@dispatcher_type", subscription.DispatcherTypeName);
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

    public class SqlSubscriptionPersistenceService : SubscriptionPersistenceService
    {
        public SqlSubscriptionPersistenceService(string connectionString)
        {
            _db = new SqlSubscriptionDB(connectionString);
        }

        SqlSubscriptionDB _db;        

        
        protected override IEnumerable<Endpoint> LoadEndpoints()
        {
            List<Endpoint> endpoints = new List<Endpoint>();
            endpoints.AddRange(_db.LoadListenerEndpoints().OfType<Endpoint>());
            endpoints.AddRange(_db.LoadSubscriptionEndpoints().OfType<Endpoint>());
            return endpoints;
        }


        protected override void CreateListener(ListenerEndpoint endpoint)
        {
            _db.CreateListener(endpoint);
        }

        protected override void CreateSubscription(SubscriptionEndpoint subscription)
        {
            _db.CreateSubscription(subscription);
        }

        protected override void DeleteListener(ListenerEndpoint endpoint)
        {
            _db.DeleteListener(endpoint.Id);
        }

        protected override void DeleteSubscription(SubscriptionEndpoint subscription)
        {
            _db.DeleteSubscription(subscription.Id);
        }
    }
}
