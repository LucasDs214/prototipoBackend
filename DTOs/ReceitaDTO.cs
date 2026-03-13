using System.Collections.Generic;

namespace PrototipoBackend.DTOs // <-- O SEGREDO ESTÁ AQUI NESTA LINHA!
{
    public class IngredienteDTO
    {
        public string NomeItem { get; set; } = string.Empty;
        public string Quantidade { get; set; } = string.Empty;
    }

    public class ReceitaDTO
    {
        public int CandidatoId { get; set; }
        public string NomePrato { get; set; } = string.Empty;
        public string ModoPreparo { get; set; } = string.Empty;
        public string TempoPreparo { get; set; } = string.Empty;
        
        public List<IngredienteDTO> Ingredientes { get; set; } = new List<IngredienteDTO>();
    }
}