using System.Security.Principal;
using Coleta.Models;

namespace coleta
{
    public class User
    {
        public static UserInfo GetUserInfo()
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            if (currentIdentity != null)
            {
                return new UserInfo
                {
                    Usuario = currentIdentity.Name,
                    Hostname = ComputerInfo.GetComputerName()
                };
            }
            return null;
        }
    }
}
