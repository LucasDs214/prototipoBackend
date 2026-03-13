using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrototipoBackend.Models;
using PrototipoBackend.DTOs;

namespace PrototipoBackend.Endpoints;

public static class ReceitaEndpoints
{
    public static void MapReceitaEndpoints(this WebApplication app)
    {
        // Rota que o React vai chamar: POST /receitas
        // Usamos o Supabase.Client injetado, exatamente como você fez em CandidatoEndpoints
        app.MapPost("/receitas", async (Supabase.Client supabase, ReceitaDTO request) =>
        {
            try
            {
                // 1. INSERIR A RECEITA
                var novaReceita = new Receita
                {
                    CandidatoId = request.CandidatoId,
                    NomePrato = request.NomePrato,
                    ModoPreparo = request.ModoPreparo,
                    TempoPreparo = request.TempoPreparo
                };

                // O .Insert() do Supabase insere e já devolve o objeto com o ID gerado pelo banco
                var receitaResponse = await supabase.From<Receita>().Insert(novaReceita);
                var receitaInserida = receitaResponse.Models.FirstOrDefault();

                if (receitaInserida == null)
                    return Results.Problem("Erro ao criar a receita. Nenhum dado foi retornado do Supabase.");

                // 2. INSERIR OS INGREDIENTES (Em Lote / Bulk Insert)
                if (request.Ingredientes != null && request.Ingredientes.Count > 0)
                {
                    var listaIngredientes = new List<Ingrediente>();

                    foreach (var ing in request.Ingredientes)
                    {
                        listaIngredientes.Add(new Ingrediente
                        {
                            ReceitaId = receitaInserida.Id, // Pega no ID que acabou de ser gerado acima!
                            NomeItem = ing.NomeItem,
                            Quantidade = ing.Quantidade
                        });
                    }

                    // Insere toda a lista de ingredientes de uma só vez (muito mais otimizado)
                    await supabase.From<Ingrediente>().Insert(listaIngredientes);
                }

                return Results.Ok(new { Mensagem = "Receita e ingredientes salvos com sucesso!", ReceitaId = receitaInserida.Id });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Erro ao salvar receita: {ex.Message}");
            }
        });
    }
}