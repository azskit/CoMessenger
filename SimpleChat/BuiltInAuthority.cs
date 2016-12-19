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
using SimpleChat.Identity;

namespace SimpleChat
{
    public class BuiltInUser : CMUser
    {
        public string Password { get; set; }

        internal override bool CheckPassword(string password)
        {
            if (Password == MD5Helper.CreateMD5(password))   //Верный пароль
                return true;
            else if (Password == String.Empty && password == String.Empty) //Пароль не задан
                return true;
            else
                return false;
        }
    }

    static class  BuiltInAuthority
    {
        /// <summary>
        /// Сериализатор списка пользователей
        /// </summary>
        private static XmlSerializer serializer = new XmlSerializer(typeof(List<BuiltInUser>));

//        /// <summary>
//        /// Создать и сохранить пример списка встроенных пользователей
//        /// </summary>
//        /// <param name="builtInUsersFileName">Файл куда сохранить новый список</param>
//        public static void MakeBlancBuiltInUsersFile(string builtInUsersFileName = "builtinusers.xml")
//        {

//            List<CMUser> UserList = new List<CMUser>();

//            UserList.Add(new CMUser()
//            {
//                DisplayName     = "Пупкин Василий Иванович",
//                UserName           = "Pupkin",
//                UserId          = Guid.NewGuid().ToString(),
//                Password    = MD5Helper.CreateMD5("abc123")
//            });



//            try
//            {
//                using (StreamWriter writer = new StreamWriter(builtInUsersFileName))
//                {
//                    serializer.Serialize(writer, UserList);
//                }
//            }
//            catch (Exception)
//            {
////#if DEBUG
//                Trace.TraceError("Ошибка создания списк пользователей");
//                throw;
////#endif
//            }
//        }

        /// <summary>
        /// Получить список пользователей из файла usersListFile
        /// </summary>
        /// <param name="usersListFile">Файл списка пользователей</param>
        /// <returns>Список пользователей</returns>
        public static IEnumerable<CMUser> GetBuiltInUsers(string usersListFile)
        {
            List<BuiltInUser> UserList = new List<BuiltInUser>();

            if (File.Exists(usersListFile))
            {

                try
                {
                    using (Stream fs = new FileStream(usersListFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (XmlReader xml_reader = new XmlTextReader(fs))
                    {
                        return (UserList = serializer.Deserialize(xml_reader) as List<BuiltInUser>);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(CustomUtilites.FormatException(e));
                    return new List<CMUser>();
                }
            }
            else
                throw new FileNotFoundException("Не найден файл списка пользователей", usersListFile);
        }



    }



}
