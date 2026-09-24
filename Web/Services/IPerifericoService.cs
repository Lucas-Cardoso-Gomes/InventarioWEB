using System.Collections.Generic;
using Web.Models;

namespace Web.Services
{
    public interface IPerifericoService
    {
        List<Periferico> GetFilteredPerifericos(string searchString, string userCpf, bool isColaboradorOnly, bool isCoordenadorOnly);
        Periferico FindById(string partNumber);
        List<Colaborador> GetColaboradores();
        void Create(Periferico periferico);
        void Update(Periferico periferico);
        void Delete(string partNumber);
        (int adicionados, int atualizados, List<string> invalidCpfs) ImportList(List<Periferico> perifericos);
    }
}
