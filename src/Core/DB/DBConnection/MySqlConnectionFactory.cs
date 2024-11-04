﻿using MySql.Data.MySqlClient;
using RoomSchedulerAPI.Core.DB.DBConnection.Interface;
using System.Data;

namespace RoomSchedulerAPI.Core.DB.DBConnection;

public class MySqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}