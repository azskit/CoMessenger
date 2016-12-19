using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Diagnostics;
using CorporateMessengerLibrary;
using SimpleChat.Identity;

namespace SimpleChat
{
    class LDAPUser : CMUser
    {
        internal override bool CheckPassword(string password)
        {
            throw new NotImplementedException();
        }
    }

    class LdapAuthority
    {
        /// <summary>
        /// Поток для информационных сообщений
        /// </summary>
        public TextWriter InfoStream{ get; set; }

        /// <summary>
        /// Поток для  сообщений об ошибках
        /// </summary>
        public TextWriter ErrorStream { get; set; }
        

        /// <summary>
        /// Сериализатор плана пользователей
        /// </summary>
        private static XmlSerializer serializer = new XmlSerializer(typeof(UsersPlan));

        /// <summary>
        /// Создать и сохранить пример плана пользователей
        /// </summary>
        /// <param name="UsersPlanFileName">Файл куда сохранить новый план</param>
        public static void MakeBlancUsersPlan(string UsersPlanFileName = "usersplan.xml")
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
#if DEBUG
                Trace.TraceError("Ошибка создания плана пользователей");
                throw;
#endif
            }
        }


        public static UsersPlan GetUsersPlan(string UsersPlanFileName = "usersplan.xml")
        {
            UsersPlan Users = new UsersPlan();

            if (File.Exists(UsersPlanFileName))
            {
                using (Stream fs = new FileStream(UsersPlanFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XmlReader xml_reader = new XmlTextReader(fs))
                {
                    return (Users = serializer.Deserialize(xml_reader) as UsersPlan);
                }
            }
            else
                throw new FileNotFoundException("Не найден файл плана пользователей", UsersPlanFileName);
        }

        /// <summary>
        /// Список всех пользователей согласно плану пользователей
        /// </summary>
        /// <param name="Users"></param>
        /// <returns></returns>
        public IEnumerable<CMUser> GetWindowsUsers(UsersPlan Users)
        {
            List<CMUser> UserList = new List<CMUser>();

            foreach (DomainEntry dom in Users.DomainList)
            {
                int userscount = 0;

                if (InfoStream != null)
                    InfoStream.WriteLine("Looking for users in {0}\\{1}", dom.DomainName, dom.MainGroup.GroupName);

                PrincipalContext ctx = null;
                try
                {
                    ctx = new PrincipalContext(ContextType.Domain, dom.DomainName);

                    GroupPrincipal principal = GroupPrincipal.FindByIdentity(ctx, dom.MainGroup.GroupName);

                    principal.GetMembers(recursive: true).
                                            OfType<UserPrincipal>().
                                            ToList().
                                            ForEach((member) =>
                                            {
                                                UserList.Add(new LDAPUser()
                                                {
                                                    DisplayName = member.DisplayName ?? member.SamAccountName,
                                                    UserId = member.Sid.Value,
                                                    UserName = member.SamAccountName,
                                                    Domain = dom.DomainName
                                                }); userscount++;
                                            });

                    if (InfoStream != null)
                        InfoStream.WriteLine("{0} users found", userscount);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(CustomUtilites.FormatException(e));
                }
                finally
                {
                    if (ctx != null) ctx.Dispose();
                }
            }

            return UserList;
        }



        #region Статические методы

        /// <summary>
        /// Получить все группы домена
        /// </summary>
        /// <param name="DomainName">Имя домена</param>
        /// <returns>Коллекция строк</returns>
        public static IEnumerable<string> GetDomainGroups(string DomainName)
        {
            using (var context = new PrincipalContext(ContextType.Domain, DomainName))
            using (var queryFilter = new GroupPrincipal(context))
            using (var searcher = new PrincipalSearcher(queryFilter))
            {
                foreach (var result in searcher.FindAll())
                {
                    yield return result.SamAccountName;
                    result.Dispose();
                }
            }
        }

        /// <summary>
        /// Принадлежит ли пользователь группе
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static bool IsUserMemberOf(string userName, string groupName)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain))
            using (var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, groupName))
            using (var userPrincipal = UserPrincipal.FindByIdentity(ctx, userName))
            {
                return userPrincipal.IsMemberOf(groupPrincipal);
            }
        }

        /// <summary>
        /// Группы, в которые входит пользователь
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetUserGroups(string userName)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain))
            using (var userPrincipal = UserPrincipal.FindByIdentity(ctx, userName))
            {
                return userPrincipal.GetGroups().Select(d => d.SamAccountName).ToList();
            }
        }
        /*
        /// <summary>
        /// Пользователи, которые входят в группу
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetGroupUsers(string groupName, string DomainName)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain, DomainName))
            using (var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, groupName))
            {
                return groupPrincipal.Members.Select(d => d.DistinguishedName).ToList();
            }
        }
        */

        public IEnumerable<CMGroup> GetGroups(UsersPlan Plan)
        {
            List<CMGroup> GroupList = new List<CMGroup>();

            foreach (DomainEntry domainEntry in Plan.DomainList)
            {
                PrincipalContext ctx = null;
                try
                {
                    ctx = new PrincipalContext(ContextType.Domain, domainEntry.DomainName);

                    foreach (GroupEntry groupEntry in domainEntry.PermanentRoomGroups)
                    {

                        try
                        {
                            if (InfoStream != null)
                                InfoStream.WriteLine("Looking for group {0}\\{1}", domainEntry.DomainName, groupEntry.GroupName);

                            GroupPrincipal principal = GroupPrincipal.FindByIdentity(ctx, groupEntry.GroupName);

                            if (principal == null)
                            {
                                if (InfoStream != null)
                                    InfoStream.WriteLine("Group {0}\\{1} not found", domainEntry.DomainName, groupEntry.GroupName);

                                continue;
                            }

                            CMGroup newGroup = new CMGroup();
                            newGroup.GroupId = principal.Sid.Value;
                            newGroup.DisplayName = principal.DisplayName ?? principal.Name;


                            if (InfoStream != null)
                                InfoStream.WriteLine("Looking for users in {0}\\{1}", domainEntry.DomainName, groupEntry.GroupName);

                            principal.GetMembers(recursive: true).
                                                OfType<UserPrincipal>().
                                                    ToList().
                                                    ForEach((member) =>
                                                    {
                                                        //newGroup.Users.Add(new CoMessengerUser()
                                                        //{
                                                        //    DisplayName = member.DisplayName ?? member.SamAccountName,
                                                        //    UserId = member.Sid.Value,
                                                        //    Login = member.SamAccountName,
                                                        //    Domain = domainEntry.DomainName,
                                                        //    IsBuiltIn = false
                                                        //}); userscount++;

                                                        newGroup.UserIds.Add(member.Sid.Value);
                                                    });

                            if (InfoStream != null)
                                InfoStream.WriteLine("{0} users found", newGroup.UserIds.Count);

                            GroupList.Add(newGroup);

                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(CustomUtilites.FormatException(e));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(CustomUtilites.FormatException(e));
                }
                finally
                {
                    if (ctx != null) ctx.Dispose();
                }
            }

            return GroupList;
        }




        /// <summary>
        /// Список доменов сети
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetDomainList()
        {

            using (DirectoryEntry en = new DirectoryEntry("LDAP://"))
            using (DirectorySearcher srch = new DirectorySearcher("objectCategory=Domain"))
            {
                SearchResultCollection coll;

                try
                {
                    coll = srch.FindAll();
                }
                catch (Exception)
                {
                    yield break;
                }
                // Enumerate over each returned domainEntry.
                foreach (SearchResult rs in coll)
                {
                    ResultPropertyCollection resultPropColl = rs.Properties;
                    foreach (object domainName in resultPropColl["name"])
                    {
                        yield return domainName.ToString();
                    }
                }
            }
        }

        #endregion
    }



    #region Структура настроечного XML файла
    public class UserEntry
    {
        public UserPrincipal Principal;
    }


    public class GroupEntry
    {
        [XmlAttribute]
        public string GroupName;
        public string DisplayName;

        //[XmlIgnore]
        //public GroupPrincipal usersList;
        //public GroupPrincipal UsersList
        //{
        //    get
        //    {
        //        if (usersList == null)
        //        {
        //            PrincipalContext ctx = null;
        //            try
        //            {
        //                ctx = new PrincipalContext(ContextType.Domain, dom.DomainName);
        //            }
        //        }
        //    }
        //}
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




}
