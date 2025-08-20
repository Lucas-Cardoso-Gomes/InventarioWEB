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
        public GpuInfo GPU { get; set; }
        public List<NetworkAdapterInfo> AdaptadoresRede { get; set; }
    }

    public class GpuInfo
    {
        public string Nome { get; set; }
        public string Fabricante { get; set; }
        public string RamDedicadaGB { get; set; }
    }

    public class NetworkAdapterInfo
    {
        public string Descricao { get; set; }
        public string EnderecoIP { get; set; }
        public string MascaraSubRede { get; set; }
        public string GatewayPadrao { get; set; }
        public string ServidoresDNS { get; set; }
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
        public List<DiskInfo> Discos { get; set; }
    }

    public class DiskInfo
    {
        public string Letra { get; set; }
        public string TotalGB { get; set; }
        public string LivreGB { get; set; }
    }
}
