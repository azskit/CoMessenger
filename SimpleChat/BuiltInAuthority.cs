using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Security.Cryptography;
using CorporateMessengerLibrary;

namespace SimpleChat
{
    static class  BuiltInAuthority
    {
        /// <summary>
        /// Сериализатор списка пользователей
        /// </summary>
        private static XmlSerializer serializer = new XmlSerializer(typeof(List<CoMessengerUser>));

        /// <summary>
        /// Создать и сохранить пример списка встроенных пользователей
        /// </summary>
        /// <param name="builtInUsersFileName">Файл куда сохранить новый список</param>
        public static void MakeBlancBuiltInUsersFile(string builtInUsersFileName = "builtinusers.xml")
        {

            List<CoMessengerUser> UserList = new List<CoMessengerUser>();

            UserList.Add(new CoMessengerUser()
            {
                DisplayName     = "Пупкин Василий Иванович",
                Login           = "Pupkin",
                UserId          = Guid.NewGuid().ToString(),
                Password    = MD5Helper.CreateMD5("abc123")
            });



            try
            {
                using (StreamWriter writer = new StreamWriter(builtInUsersFileName))
                {
                    serializer.Serialize(writer, UserList);
                }
            }
            catch (Exception)
            {
//#if DEBUG
                Trace.TraceError("Ошибка создания списк пользователей");
                throw;
//#endif
            }
        }

        /// <summary>
        /// Получить список пользователей из файла usersListFile
        /// </summary>
        /// <param name="usersListFile">Файл списка пользователей</param>
        /// <returns>Список пользователей</returns>
        public static IEnumerable<CoMessengerUser> GetBuiltInUsers(string usersListFile)
        {
            List<CoMessengerUser> UserList = new List<CoMessengerUser>();

            if (File.Exists(usersListFile))
            {
                using (Stream fs = new FileStream(usersListFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XmlReader xml_reader = new XmlTextReader(fs))
                {
                    return (UserList = serializer.Deserialize(xml_reader) as List<CoMessengerUser>);
                }
            }
            else
                throw new FileNotFoundException("Не найден файл списк пользователей", usersListFile);
        }



    }



}
