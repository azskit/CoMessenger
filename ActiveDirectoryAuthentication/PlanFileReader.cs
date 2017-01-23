using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ActiveDirectoryAuthentication
{
    public class PlanFileReader
    {
        /// <summary>
        /// Сериализатор плана пользователей
        /// </summary>
        private static XmlSerializer serializer = new XmlSerializer(typeof(UsersPlan));

        #region Структура настроечного XML файла

        public class GroupEntry
        {
            [XmlAttribute]
            public string GroupName;
            public string DisplayName;
        }

        public class DomainEntry
        {
            [XmlAttribute]
            public string DomainName;

            public GroupEntry MainGroup;

            public List<GroupEntry> PermanentRoomGroups;
        }


        [Serializable]
        public class UsersPlan
        {
            public List<DomainEntry> DomainList;
        }
        #endregion


        public static UsersPlan GetUsersPlan(string UsersPlanFileName = "ActiveDirectoryUsers.xml")
        {
            UsersPlan Users = new UsersPlan();

            if (File.Exists(UsersPlanFileName))
            {
                using (Stream fs = new FileStream(UsersPlanFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                //using (XmlReader xml_reader = new XmlTextReader(fs))
                {
                    return (Users = serializer.Deserialize(fs) as UsersPlan);
                }
            }
            else
                throw new FileNotFoundException("Не найден файл плана пользователей", UsersPlanFileName);
        }

        /// <summary>
        /// Создать и сохранить пример плана пользователей
        /// </summary>
        /// <param name="UsersPlanFileName">Файл куда сохранить новый план</param>
        public static void MakeBlancUsersPlan(string UsersPlanFileName = "ActiveDirectoryUsers_Example.xml")
        {

            UsersPlan Users = new UsersPlan();

            Users.DomainList = new List<DomainEntry>();

            Users.DomainList.Add(new DomainEntry() { DomainName = "Contoso" });

            Users.DomainList[0].MainGroup = new GroupEntry() { GroupName = "Domain Users" };

            Users.DomainList[0].PermanentRoomGroups = new List<GroupEntry>();

            Users.DomainList[0].PermanentRoomGroups.Add(new GroupEntry() { GroupName = "Administrators" });
            Users.DomainList[0].PermanentRoomGroups.Add(new GroupEntry() { GroupName = "Sales" });
            Users.DomainList[0].PermanentRoomGroups.Add(new GroupEntry() { GroupName = "Managers" });

            try
            {
                using (StreamWriter writer = new StreamWriter(UsersPlanFileName))
                {
                    serializer.Serialize(writer, Users);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
