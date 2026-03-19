using System;
using System.Linq;
using System.Management;
using System.Text;
using Coleta.Models;

namespace coleta
{
    public class RAM
    {
        public static RamInfo GetRamInfo()
        {
            try
            {
                string ramSizeMB = "N/A";
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                    if (result != null)
                    {
                        ulong ramSize = Convert.ToUInt64(result["TotalPhysicalMemory"]);
                        ramSizeMB = $"{ramSize / (1024 * 1024)} MB";
                    }
                }

                return new RamInfo
                {
                    RamTotal = ramSizeMB,
                    Tipo = GetRamType(),
                    Velocidade = GetRamSpeed(),
                    Voltagem = GetRamVoltage(),
                    PorModulo = GetRamPorModule()
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetRamType()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                    if (result != null)
                    {
                        int memoryType = Convert.ToInt32(result["MemoryType"]);
                        return memoryType switch
                        {
                            0 => "Tipo de RAM desconhecido",
                            1 => "Outro",
                            2 => "DRAM",
                            3 => "Synchronous DRAM",
                            4 => "Cache DRAM",
                            5 => "EDO",
                            6 => "EDRAM",
                            7 => "VRAM",
                            8 => "SRAM",
                            9 => "RAM",
                            10 => "ROM",
                            11 => "Flash",
                            12 => "EEPROM",
                            13 => "FEPROM",
                            14 => "EPROM",
                            15 => "CDRAM",
                            16 => "3DRAM",
                            17 => "SDRAM",
                            18 => "SGRAM",
                            19 => "RDRAM",
                            20 => "DDR",
                            21 => "DDR2",
                            22 => "DDR2 FB-DIMM",
                            24 => "DDR3",
                            25 => "FBD2",
                            26 => "DDR4",
                            27 => "LPDDR",
                            28 => "LPDDR2",
                            29 => "LPDDR3",
                            30 => "LPDDR4",
                            31 => "DDR5",
                            _ => $"{memoryType} Tipo desconhecido"
                        };
                    }
                }
            }
            catch (Exception) { /* ignore */ }
            return "Tipo de RAM não disponível";
        }

        private static string GetRamSpeed()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                    if (result != null)
                    {
                        uint ramSpeed = Convert.ToUInt32(result["ConfiguredClockSpeed"]);
                        return $"{ramSpeed} MHz";
                    }
                }
            }
            catch (Exception) { /* ignore */ }
            return "Velocidade de RAM não disponível";
        }

        private static string GetRamVoltage()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                    if (result != null)
                    {
                        uint ramVoltage = Convert.ToUInt32(result["ConfiguredVoltage"]);
                        return $"{ramVoltage} Volts";
                    }
                }
            }
            catch (Exception) { /* ignore */ }
            return "Voltagem de RAM não disponível";
        }
        private static string GetRamPorModule()
        {
            try
            {
                var capacidades = new StringBuilder();
                int i = 0;

                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        capacidades.Append($"Módulo {i}: {Convert.ToUInt64(obj["Capacity"]) / (1024 * 1024)} MB; ");
                        i++;
                    }
                }

                return capacidades.ToString().TrimEnd(';',' ');
            }
            catch (Exception) { /* ignore */ }
            return "N/A";
        }
    }
}
