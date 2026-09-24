using System.Collections.Generic;
using Web.Models;
using Monitor = Web.Models.Monitor;

namespace Web.Services
{
    public interface IMonitorService
    {
        MonitorIndexViewModel GetFilteredMonitores(List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos, string userCpf, bool isColaboradorOnly, bool isCoordenadorOnly);
        Monitor FindById(string partNumber);
        List<Colaborador> GetColaboradores();
        void Create(Monitor monitor);
        void Update(Monitor monitor);
        void Delete(string partNumber);
        (int adicionados, int atualizados, List<string> invalidCpfs) ImportList(List<Monitor> monitores);
    }
}
