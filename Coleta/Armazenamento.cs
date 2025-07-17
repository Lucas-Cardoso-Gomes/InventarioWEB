using System.Management;
using System.Text;

namespace coleta
{
    public class Armazenamento
    {
        public static string GetStorageInfo()
        {
            var ArmazenamentoBuilder = new StringBuilder();
            bool unidadeDEncontrada = false;
            bool unidadeCEncontrada = false;

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"))
            {
                foreach (var result in searcher.Get())
                {
                    string letra = result["Name"].ToString();

                    if (letra.Equals("C:") || letra.Equals("D:"))
                    {
                        ulong total = Convert.ToUInt64(result["Size"]);
                        ulong livre = Convert.ToUInt64(result["FreeSpace"]);

                        string armazenamento = $"{letra}\n{total / (1024 * 1024 * 1024)} GB\n{livre / (1024 * 1024 * 1024)} GB";
                        ArmazenamentoBuilder.AppendLine(armazenamento);

                        if (letra.Equals("C:"))
                        {
                            unidadeCEncontrada = true;
                        }
                        else if (letra.Equals("D:"))
                        {
                            unidadeDEncontrada = true;
                        }
                    }
                }
            }

            if (!unidadeCEncontrada)
            {
                string armazenamentoC = "C:\n0 GB\n0 GB";
                ArmazenamentoBuilder.AppendLine(armazenamentoC);
            }

            if (!unidadeDEncontrada)
            {
                string armazenamentoD = "D:\n0 GB\n0 GB";
                ArmazenamentoBuilder.AppendLine(armazenamentoD);
            }

            if (ArmazenamentoBuilder.Length > 0)
            {
                return ArmazenamentoBuilder.ToString();
            }
            else
            {
                return "Informação de armazenamento não disponível";
            }
        }
    }
}
