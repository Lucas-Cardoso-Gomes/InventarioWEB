using System;
using System.IO;
using System.Diagnostics;

namespace coleta
{
    public class Comandos
    {
        public static string ExecutarComando(string comando)
        {
            if (string.IsNullOrWhiteSpace(comando))
            {
                Console.WriteLine("[WARN] Recebido um comando vazio.");
                return "Comando não pode ser vazio.";
            }

            Console.WriteLine($"[CMD] Executando comando: '{comando}'");
            try
            {
                Process processo = new Process();
                processo.StartInfo.FileName = "cmd.exe";
                processo.StartInfo.Arguments = $"/c {comando}";
                processo.StartInfo.RedirectStandardOutput = true;
                processo.StartInfo.RedirectStandardError = true;
                processo.StartInfo.UseShellExecute = false;
                processo.StartInfo.CreateNoWindow = true;

                processo.Start();

                var outputTask = processo.StandardOutput.ReadToEndAsync();
                var errorTask = processo.StandardError.ReadToEndAsync();

                if (!processo.WaitForExit(30000)) // Timeout de 30 segundos
                {
                    try
                    {
                        processo.Kill();
                        Console.WriteLine("[CMD-ERROR] O processo excedeu o tempo limite e foi encerrado.");
                    }
                    catch (Exception killEx)
                    {
                         Console.WriteLine($"[CMD-FATAL] Falha ao encerrar o processo que excedeu o tempo limite: {killEx.Message}");
                    }
                    return "Erro: O comando demorou muito para executar (timeout de 30s).";
                }

                string output = outputTask.Result;
                string error = errorTask.Result;

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"[CMD-ERROR] Erro ao executar comando: {error}");
                    return $"Erro: {error}";
                }

                Console.WriteLine($"[CMD-RESULT] Resultado: {output}");
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CMD-FATAL] Exceção ao executar o comando: {ex.Message}");
                return $"Erro fatal ao executar o comando: {ex.Message}";
            }
        }
    }
}

