using PrototipoBackend.Models;
using PrototipoBackend.DTOs;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security; // Adicionado para a segurança TLS

namespace PrototipoBackend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // --- 1. LOGIN ---
        app.MapPost("/login", async (Supabase.Client supabase, LoginRequest request) =>
        {
            try
            {
                var resposta = await supabase.From<Usuario>().Where(x => x.Cpf == request.Cpf).Get();
                var usuario = resposta.Models.FirstOrDefault();

                if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                    return Results.BadRequest("CPF ou senha incorretos.");

                return Results.Ok(new { 
                    perfil = usuario.Perfil, 
                    nome = usuario.Nome, 
                    email = usuario.Email,
                    precisaTrocarSenha = usuario.SenhaTemporaria 
                });
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 2. NOVA ROTA: ESQUECI MINHA SENHA (CORRIGIDA PARA A NUVEM) ---
        app.MapPost("/esqueci-senha", async (Supabase.Client supabase, EsqueciSenhaRequest request) =>
        {
            try
            {
                var resposta = await supabase.From<Usuario>().Where(x => x.Cpf == request.Cpf).Get();
                var usuario = resposta.Models.FirstOrDefault();

                if (usuario == null) return Results.BadRequest("CPF não encontrado no sistema.");

                // Gera uma senha aleatória de 8 caracteres
                string senhaProvisoria = Guid.NewGuid().ToString().Substring(0, 8);

                // Atualiza a senha no banco e liga o booleano de senha temporária
                await supabase.From<Usuario>()
                              .Where(x => x.Cpf == request.Cpf)
                              .Set(x => x.Senha, BCrypt.Net.BCrypt.HashPassword(senhaProvisoria))
                              .Set(x => x.SenhaTemporaria, true)
                              .Update();

                // Envia o e-mail
                var mensagem = new MimeMessage();
                
                // Lê o e-mail configurado no Render. Se não achar, usa o teu por defeito.
                var emailRemetente = Environment.GetEnvironmentVariable("EmailConfig__Email") ?? "lucasds151@gmail.com";
                var senhaApp = Environment.GetEnvironmentVariable("EmailConfig__Senha") ?? "svaiojacveepmiis";

                mensagem.From.Add(new MailboxAddress("MerendaChef", emailRemetente));
                mensagem.To.Add(new MailboxAddress(usuario.Nome, usuario.Email)); 
                mensagem.Subject = "Recuperação de Senha - MerendaChef";
                mensagem.Body = new TextPart("html") { 
                    Text = $"<h2>Olá, {usuario.Nome}</h2><p>Sua senha provisória é: <b>{senhaProvisoria}</b></p><p>Ao fazer o login, você deverá cadastrar uma nova senha.</p>" 
                };

                using var client = new SmtpClient();
                client.Timeout = 15000;
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                
                // CORREÇÃO 1: Usar SecureSocketOptions.StartTls em vez de 'false'
                await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                
                // CORREÇÃO 2: Passar as credenciais puxadas do Render
                await client.AuthenticateAsync(emailRemetente, senhaApp);
                
                // CORREÇÃO 3: Operações Assíncronas
                await client.SendAsync(mensagem);
                await client.DisconnectAsync(true);

                return Results.Ok("Uma senha provisória foi enviada para o seu e-mail cadastrado.");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 3. NOVA ROTA: TROCAR SENHA PROVISÓRIA ---
        app.MapPost("/trocar-senha", async (Supabase.Client supabase, TrocarSenhaRequest request) =>
        {
            try
            {
                await supabase.From<Usuario>()
                              .Where(x => x.Cpf == request.Cpf)
                              .Set(x => x.Senha, BCrypt.Net.BCrypt.HashPassword(request.NovaSenha))
                              .Set(x => x.SenhaTemporaria, false)
                              .Update();

                return Results.Ok("Senha alterada com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 4. REGISTRAR USUÁRIO COMUM (ADMIN/PROFESSORES) ---
        app.MapPost("/registrar-usuario", async (Supabase.Client supabase, RegistroRequest request) =>
        {
            try
            {
                var novoUsuario = new Usuario { 
                    Nome = request.Nome,
                    Email = request.Email,
                    Cpf = request.Cpf,
                    Perfil = request.Perfil, 
                    Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha) 
                };
                await supabase.From<Usuario>().Insert(novoUsuario);
                return Results.Ok("Usuário cadastrado com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 5. CADASTRAR NOVO CANDIDATO ---
        app.MapPost("/cadastrar-candidato", async (Supabase.Client supabase, CadastroCompletoRequest request) =>
        {
            try
            {
                var novoUsuario = new Usuario { 
                    Nome = request.Nome, 
                    Email = request.Email,
                    Cpf = request.Cpf, 
                    Perfil = "candidato", 
                    Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha) 
                };
                await supabase.From<Usuario>().Insert(novoUsuario);

                var novoCandidato = new Candidato { 
                    Nome = request.Nome, 
                    Cpf = request.Cpf,
                    Telefone = request.Telefone, 
                    Unidade = request.Unidade, 
                    Email = request.Email, 
                    FotoUrl = request.FotoUrl, 
                    Validado = false 
                };
                await supabase.From<Candidato>().Insert(novoCandidato);

                return Results.Ok("Conta criada e inscrição realizada com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });
    }
}