using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;

namespace ActiveDirectoryAuthentication
{
    public static class ActiveDirectoryHelper
    {

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
    }
}
