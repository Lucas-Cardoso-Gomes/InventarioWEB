using System.Security.Principal;

namespace coleta
{
    public class User
    {
        public static string GetUserInfo()
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            if (currentIdentity != null)
            {
                string userName = currentIdentity.Name;
                int index = userName.IndexOf("\\");
                if (index >= 0 && index < userName.Length - 1)
                {
                    string domain = userName.Substring(0, index);
                    string user = userName.Substring(index + 1);
                    string computerName = ComputerInfo.GetComputerName();
                    
                    return $"{domain}\\{user}\n{computerName}";
                }
            }
            return ("Domínio ou usuariop não Identificado");
        }
    }
}
