using Postgrest.Attributes;
using Postgrest.Models;

namespace PrototipoBackend.Models;

// O [Table] diz ao C# exatamente qual o nome da tabela lá no Supabase
[Table("receitas")]
public class Receita : BaseModel
{
    // O false significa que o próprio Supabase vai gerar este ID automaticamente (auto-incremento)
    [PrimaryKey("id", false)] 
    public int Id { get; set; }

    [Column("candidato_id")]
    public int CandidatoId { get; set; }

    [Column("nome_prato")]
    public string NomePrato { get; set; } = string.Empty;

    [Column("modo_preparo")]
    public string ModoPreparo { get; set; } = string.Empty;

    [Column("tempo_preparo")]
    public string TempoPreparo { get; set; } = string.Empty;
}