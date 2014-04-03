using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace SLApp_Beta
{
    class PasswordMethods
    {
        private static Random random;
        
        public PasswordMethods() {
            random = new Random((int)DateTime.Now.Ticks);
        }
        
        public bool verifyPassword(string databaseHash, string userPassword)
        {
            string salt = databaseHash.Substring(0,16);

            byte[] password = Encoding.UTF8.GetBytes(userPassword);
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hashedPass = GenerateSaltedHash(password,saltBytes);
            
            string userHash = salt+"."+Convert.ToBase64String(hashedPass);
            
            return (userHash == databaseHash);
        }
    
        public string saltAndHashPassword(string password) {
            byte[] passwordByte = Encoding.UTF8.GetBytes(password);
            string Rand1 = RandomString(10);
            byte[] salt = Encoding.UTF8.GetBytes(Rand1);
            byte[] hashedPass = GenerateSaltedHash(passwordByte,salt);
            return Convert.ToBase64String(salt)+"."+Convert.ToBase64String(hashedPass);
        }
        
        // http://stackoverflow.com/a/1122519/557358
        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));                 
                builder.Append(ch);
            }
    
            return builder.ToString();
        }
    
        
        // http://stackoverflow.com/a/2138588/557358
        private byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {            
          HashAlgorithm algorithm = new SHA256Managed();
        
          byte[] plainTextWithSaltBytes = 
            new byte[plainText.Length + salt.Length];
        
          for (int i = 0; i < plainText.Length; i++)
          {
            plainTextWithSaltBytes[i] = plainText[i];
          }
          for (int i = 0; i < salt.Length; i++)
          {
            plainTextWithSaltBytes[plainText.Length + i] = salt[i];
          }
        
          return algorithm.ComputeHash(plainTextWithSaltBytes);            
        } 
     
    }    
}
