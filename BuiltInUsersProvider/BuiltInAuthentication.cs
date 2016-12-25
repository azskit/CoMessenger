using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Serialization;
using CimPlugin.Plugin.Authentication;
using CorporateMessengerLibrary;

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
                    using (XmlReader xml_reader = new XmlTextReader(fs))
                    {
                        return serializer.Deserialize(xml_reader) as List<BuiltInUser>;
                    }
                }
                catch (Exception e)
                {
                    ErrorStream?.WriteLine("An error occured while reading file {0} : {1}", usersListFile, e.Message);
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
}
