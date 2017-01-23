using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CimPlugin.Plugin.Authentication;
using CimPlugin.Plugin.Groups;

namespace ActiveDirectoryAuthentication
{
    [Export(typeof(IAuthentication))]
    [Export(typeof(IGroupCollector))]
    public class ActiveDirectoryAuthentication : IAuthentication, IGroupCollector
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
                return "ActiveDirectoryAuthentication";
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
            if (plan == null)
                InitUsersPlan();

            return GetWindowsUsers(plan);
        }

        #endregion

        #region IGroupCollector
        public IEnumerable<Group> CollectGroups()
        {
            if (plan == null)
                InitUsersPlan();

            return GetGroups(plan);
        }
        #endregion



        PlanFileReader.UsersPlan plan;

        private void InitUsersPlan()
        {
            try
            {
                plan = PlanFileReader.GetUsersPlan(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActiveDirectoryUsers.xml"));
            }
            catch (FileNotFoundException)
            {
                ErrorStream?.WriteLine("File ActiveDirectoryUsers.xml is not found");
                throw;
            }
        }

        /// <summary>
        /// Список всех пользователей согласно плану пользователей
        /// </summary>
        /// <param name="Users"></param>
        /// <returns></returns>
        public IEnumerable<ActiveDirectoryUser> GetWindowsUsers(PlanFileReader.UsersPlan Users)
        {
            List<ActiveDirectoryUser> UserList = new List<ActiveDirectoryUser>();

            foreach (PlanFileReader.DomainEntry dom in Users.DomainList)
            {
                int userscount = 0;

                InfoStream?.WriteLine("Looking for users in {0}\\{1}", dom.DomainName, dom.MainGroup.GroupName);

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
                                                UserList.Add(new ActiveDirectoryUser()
                                                {
                                                    DisplayName = member.DisplayName ?? member.SamAccountName,
                                                    UserId = member.Sid.Value,
                                                    UserName = member.SamAccountName,
                                                    Domain = dom.DomainName
                                                }); userscount++;
                                            });

                    InfoStream?.WriteLine("{0} users found", userscount);
                }
                catch (PrincipalServerDownException e)
                {
                    ErrorStream?.WriteLine(e.ToString());
                }
                catch (Exception e)
                {
                    //ErrorStream?.WriteLine(CustomUtilites.FormatException(e));
                    ErrorStream?.WriteLine(e.ToString());
                    throw;
                }
                finally
                {
                    ctx?.Dispose();
                }
            }

            return UserList;
        }

        public IEnumerable<Group> GetGroups(PlanFileReader.UsersPlan Plan)
        {
            List<Group> GroupList = new List<Group>();

            foreach (PlanFileReader.DomainEntry domainEntry in Plan.DomainList)
            {
                PrincipalContext ctx = null;
                try
                {
                    ctx = new PrincipalContext(ContextType.Domain, domainEntry.DomainName);

                    foreach (PlanFileReader.GroupEntry groupEntry in domainEntry.PermanentRoomGroups)
                    {

                        try
                        {
                            InfoStream?.WriteLine("Looking for group {0}\\{1}", domainEntry.DomainName, groupEntry.GroupName);

                            GroupPrincipal principal = GroupPrincipal.FindByIdentity(ctx, groupEntry.GroupName);

                            if (principal == null)
                            {
                                WarningStream?.WriteLine("Group {0}\\{1} not found", domainEntry.DomainName, groupEntry.GroupName);

                                continue;
                            }

                            ActiveDirectoryGroup newGroup = new ActiveDirectoryGroup();
                            newGroup.GroupId = principal.Sid.Value;
                            newGroup.DisplayName = principal.DisplayName ?? principal.Name;

                            InfoStream?.WriteLine("Looking for users in {0}\\{1}", domainEntry.DomainName, groupEntry.GroupName);

                            principal.GetMembers(recursive: true).
                                                OfType<UserPrincipal>().
                                                    ToList().
                                                    ForEach((member) =>
                                                    {
                                                        newGroup.UserIds.Add(member.Sid.Value);
                                                    });

                            InfoStream?.WriteLine("{0} users found", newGroup.UserIds.Count);

                            GroupList.Add(newGroup);

                        }
                        catch (Exception e)
                        {
                            //Console.Error.WriteLine(CustomUtilites.FormatException(e));
                            ErrorStream?.WriteLine(e.ToString());
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorStream?.WriteLine(e);
                }
                finally
                {
                    ctx?.Dispose();
                }
            }

            return GroupList;
        }



        public class ActiveDirectoryGroup : CimPlugin.Plugin.Groups.Group
        {        }




        public class ActiveDirectoryUser : AuthenticationData
        {
            //internal override bool CheckPassword(string password)
            //{
            //    throw new NotImplementedException();
            //}

            public override bool CheckPassword(string password, out string error)
            {
                error = "";
                throw new NotImplementedException();
            }
        }

    }
}
