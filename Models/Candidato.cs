using Postgrest.Attributes;
using Postgrest.Models;

namespace PrototipoBackend.Models;

[Table("candidatos")] 
public class Candidato : BaseModel
{
    [PrimaryKey("id", false)] public long Id { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("Nome")] public string Nome { get; set; }
    [Column("Telefone")] public string Telefone { get; set; }
    [Column("Unidade")] public string Unidade { get; set; }
    [Column("Email")] public string Email { get; set; }
    [Column("Validado")] public bool Validado { get; set; }
    [Column("FotoUrl")] public string? FotoUrl { get; set; }
    [Column("cpf")]  public string Cpf { get; set; }

}