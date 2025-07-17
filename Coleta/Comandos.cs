using System;
using System.IO;
using System.Diagnostics;

namespace coleta
{
    public class Comandos
    {
        public static string ExecutarComando(string comando)
        {
            try
            {
                Process processo = new Process();
                processo.StartInfo.FileName = "cmd.exe";
                processo.StartInfo.Arguments = $"/c {comando}";
                processo.StartInfo.RedirectStandardOutput = true;
                processo.StartInfo.UseShellExecute = false;
                processo.StartInfo.CreateNoWindow = false;
                processo.Start();

                Console.WriteLine($"Executando comando: {comando}");

                StreamReader reader = processo.StandardOutput;
                string resultado = reader.ReadToEnd();
                Console.WriteLine($"Resultado do comando: {resultado}");

                processo.WaitForExit();

                return resultado;
            }
            catch (Exception ex)
            {
                return $"Erro ao executar o comando: {ex.Message}";
            }
        }
    }
}

