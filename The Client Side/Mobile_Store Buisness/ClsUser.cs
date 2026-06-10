using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace Mobile_Store_Buisness
{
    public class ClsUser
    {
        public static bool CheckLogin(string Email, string Password)
        {
          
            return ClsUsersData.CheckLogin(Email, Password);
           
        }
    }
}
