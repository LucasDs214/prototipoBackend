using Postgrest.Attributes;
using Postgrest.Models;

namespace PrototipoBackend.Models;

[Table("usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id", false)] public int Id { get; set; }
    
    [Column("email")] public string Email { get; set; }
    
    [Column("cpf")] public string Cpf { get; set; } // Adicionado o CPF
    
    [Column("senha")] public string Senha { get; set; }
    
    [Column("perfil")] public string Perfil { get; set; }
    
    [Column("nome")] public string Nome { get; set; }
    
    // A mágica do banco que o C# não estava achando:
    [Column("senha_temporaria")] public bool SenhaTemporaria { get; set; } 
}