using MimeKit;
using MailKit.Net.Smtp;
using PrototipoBackend.Models;
using PrototipoBackend.DTOs;

namespace PrototipoBackend.Endpoints;

public static class CandidatoEndpoints
{
    public static void MapCandidatoEndpoints(this WebApplication app)
    {
        app.MapPost("/inscrever", async (Supabase.Client supabase, CandidatoRequest request) =>
        {
            try
            {
                var novoCandidato = new Candidato { Nome = request.Nome, Telefone = request.Telefone, Unidade = request.Unidade, Email = request.Email, FotoUrl = request.FotoUrl, Validado = false };
                await supabase.From<Candidato>().Insert(novoCandidato);

                var mensagem = new MimeMessage();
                mensagem.From.Add(new MailboxAddress("MerendaChef", "lucasds151@gmail.com"));
                mensagem.To.Add(new MailboxAddress(novoCandidato.Nome, novoCandidato.Email));
                mensagem.Subject = "Inscrição Confirmada - MerendaChef";
                mensagem.Body = new TextPart("html") { Text = $"<h1>Olá, {novoCandidato.Nome}!</h1><p>Sua inscrição para a unidade <b>{novoCandidato.Unidade}</b> foi recebida!</p>" };

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("lucasds151@gmail.com", "svaiojacveepmiis");
                client.Send(mensagem);
                client.Disconnect(true);

                return Results.Ok("Candidato salvo e e-mail enviado!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        app.MapGet("/candidatos", async (Supabase.Client supabase) =>
        {
            try
            {
                var resultado = await supabase.From<Candidato>().Get();
                var candidatosLimpos = resultado.Models.Select(c => new { id = c.Id, createdAt = c.CreatedAt, nome = c.Nome, telefone = c.Telefone, unidade = c.Unidade, email = c.Email, validado = c.Validado, fotoUrl = c.FotoUrl });
                return Results.Ok(candidatosLimpos);
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        app.MapPost("/validar", async (Supabase.Client supabase, ValidarRequest request) =>
        {
            try
            {
                await supabase.From<Candidato>().Where(x => x.Email == request.Email).Set(x => x.Validado, request.Validado).Update();
                return Results.Ok("Status atualizado com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });
    }
}