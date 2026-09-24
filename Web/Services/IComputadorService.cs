using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Models;

namespace Web.Services
{
    public interface IComputadorService
    {
        ComputadorIndexViewModel GetPagedComputadores(string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes,
            List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            string userCpf, bool isColaboradorOnly, bool isCoordenadorOnly,
            int pageNumber, int pageSize);

        Computador FindById(string mac);
        List<Colaborador> GetColaboradores();
        void Create(Computador comp);
        void Update(Computador comp);
        void Delete(string mac);
        (int adicionados, int atualizados, List<string> invalidCpfs) ImportList(List<Computador> computadores);
    }
}
