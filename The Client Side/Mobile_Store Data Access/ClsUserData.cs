using Mobile_Store_Data_Access;
using System;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

public class ClsUsersData
{
    public static bool CheckLogin(string email, string password)
    {
        return Task.Run(() => ApiClient.LoginAsync(email, password)).Result;
    }
}