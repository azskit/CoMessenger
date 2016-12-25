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


    static class  BuiltInAuthority
    {
        /// <summary>
        /// Сериализатор списка пользователей
        /// </summary>

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





    }



}
