using Postgrest.Attributes;
using Postgrest.Models;

namespace PrototipoBackend.Models;

[Table("ingredientes")]
public class Ingrediente : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("receita_id")]
    public int ReceitaId { get; set; }

    [Column("nome_item")]
    public string NomeItem { get; set; } = string.Empty;

    [Column("quantidade")]
    public string Quantidade { get; set; } = string.Empty;
}