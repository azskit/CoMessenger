using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CimPlugin.Plugin.Authentication;

namespace BuiltInAuthentication
{
    [Export(typeof(IAuthentication))]
    public class BuiltInAuthentication : IAuthentication

    {
        #region IPlugin

        public string Author
        {
            get
            {
                return "azskit";
            }
        }

        public string Name
        {
            get
            {
                return "BuiltInUsersProvider";
            }
        }

        public string Version
        {
            get
            {
                return "0.0.0.1";
            }
        }

        public TextWriter WarningStream { get; set; }

        public TextWriter ErrorStream { get; set; }

        public TextWriter InfoStream { get; set; }
        #endregion



        #region IAuthentication

        public IEnumerable<AuthenticationData> CollectUsers()
        {
            //Uri pathAUri = new Uri(Assembly.GetExecutingAssembly().Location);

            return GetBuiltInUsers(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"builtinusers.xml"));
        } 
        #endregion


        /// <summary>
        /// Serializer
        /// </summary>        
        private XmlSerializer serializer = new XmlSerializer(typeof(List<BuiltInUser>));



        /// <summary>
        /// Получить список пользователей из файла usersListFile
        /// </summary>
        /// <param name="usersListFile">Файл списка пользователей</param>
        /// <returns>Список пользователей</returns>
        private IEnumerable<AuthenticationData> GetBuiltInUsers(string usersListFile)
        {
            //List<BuiltInUser> UserList = new List<BuiltInUser>();

            if (File.Exists(usersListFile))
            {

                try
                {
                    using (Stream fs = new FileStream(usersListFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    //using (XmlReader xml_reader = new XmlTextReader(fs))
                    {
                        return serializer.Deserialize(fs) as List<BuiltInUser>;
                    }
                }
                catch (Exception e)
                {
                    ErrorStream?.WriteLine("An error has occured while reading file {0} : {1}", usersListFile, e.Message);
                    return new List<AuthenticationData>();
                }
            }
            else
            {
                ErrorStream?.WriteLine("File {0} not found", usersListFile);

                return new List<AuthenticationData>();
            }
        }

        public class BuiltInUser : AuthenticationData
        {
            public string Password { get; set; }

            public override bool CheckPassword(string password, out string error)
            {
                error = "";

                if (Password == MD5Helper.CreateMD5(password))   //Верный пароль
                    return true;
                else if (Password == String.Empty && password == String.Empty) //Пароль не задан
                    return true;
                else
                    return false;
            }
        }

    }

    public static class MD5Helper
    {
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }
    }
}
