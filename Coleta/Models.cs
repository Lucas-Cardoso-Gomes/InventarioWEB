namespace Coleta.Models
{
    public class HardwareInfo
    {
        public ProcessorInfo Processador { get; set; }
        public RamInfo Ram { get; set; }
        public UserInfo Usuario { get; set; }
        public string Fabricante { get; set; }
        public string MAC { get; set; }
        public string SO { get; set; }
        public string ConsumoCPU { get; set; }
        public StorageInfo Armazenamento { get; set; }
    }

    public class ProcessorInfo
    {
        public string Nome { get; set; }
        public string Fabricante { get; set; }
        public int Cores { get; set; }
        public int Threads { get; set; }
        public string ClockSpeed { get; set; }
    }

    public class RamInfo
    {
        public string RamTotal { get; set; }
        public string Tipo { get; set; }
        public string Velocidade { get; set; }
        public string Voltagem { get; set; }
        public string PorModulo { get; set; }
    }

    public class UserInfo
    {
        public string Usuario { get; set; }
        public string Hostname { get; set; }
    }

    public class StorageInfo
    {
        public DiskInfo DriveC { get; set; }
        public DiskInfo DriveD { get; set; }
    }

    public class DiskInfo
    {
        public string Letra { get; set; }
        public string TotalGB { get; set; }
        public string LivreGB { get; set; }
    }
}
