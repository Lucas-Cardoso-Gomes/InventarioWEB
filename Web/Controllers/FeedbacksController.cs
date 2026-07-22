using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class FeedbacksController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<FeedbacksController> _logger;

        public FeedbacksController(IDatabaseService databaseService, ILogger<FeedbacksController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        // Action para Enviar Feedback (via Modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enviar([FromForm] string assunto, [FromForm] string mensagem, [FromForm] bool isAnonimo)
        {
            if (string.IsNullOrWhiteSpace(assunto) || string.IsNullOrWhiteSpace(mensagem))
            {
                TempData["ErrorMessage"] = "Assunto e mensagem são obrigatórios.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            string? cpf = null;
            if (!isAnonimo && User.Identity?.IsAuthenticated == true)
            {
                cpf = User.FindFirstValue("ColaboradorCPF");
            }

            var protocolo = GerarProtocolo();

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    var sql = @"INSERT INTO Feedbacks (Protocolo, Assunto, Mensagem, DataCriacao, UsuarioCPF, Status)
                                VALUES (@Protocolo, @Assunto, @Mensagem, @DataCriacao, @UsuarioCPF, 'Aberto')";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p0 = cmd.CreateParameter(); p0.ParameterName = "@Protocolo"; p0.Value = protocolo; cmd.Parameters.Add(p0);
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@Assunto"; p1.Value = assunto; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@Mensagem"; p2.Value = mensagem; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@DataCriacao"; p3.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@UsuarioCPF"; p4.Value = cpf ?? (object)DBNull.Value; cmd.Parameters.Add(p4);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = $"Feedback enviado! Anote seu protocolo para acompanhamento: {protocolo}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar feedback.");
                TempData["ErrorMessage"] = "Erro ao enviar feedback.";
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        // GET: Lista para Administradores
        [Authorize(Roles = "Admin,Diretoria/RH")]
        public IActionResult Index()
        {
            var feedbacks = new List<Feedback>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var sql = @"SELECT f.*, c.Nome as AutorNome
                            FROM Feedbacks f
                            LEFT JOIN Colaboradores c ON f.UsuarioCPF = c.CPF
                            ORDER BY f.DataCriacao DESC";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            feedbacks.Add(new Feedback
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Assunto = reader["Assunto"].ToString(),
                                Mensagem = reader["Mensagem"].ToString(),
                                DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                UsuarioCPF = reader["UsuarioCPF"] != DBNull.Value ? reader["UsuarioCPF"].ToString() : null,
                                Status = reader["Status"].ToString(),
                                AutorNome = reader["AutorNome"] != DBNull.Value ? reader["AutorNome"].ToString() : "Anônimo"
                            });
                        }
                    }
                }
            }
            return View(feedbacks);
        }

        private string GerarProtocolo()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return "FDK-" + new string(result);
        }

        [AllowAnonymous]
        public IActionResult Acompanhar(string protocolo = null)
        {
            if (string.IsNullOrEmpty(protocolo))
            {
                return View();
            }

            Feedback feedback = null;
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var sql = @"SELECT f.*, c.Nome as AutorNome
                            FROM Feedbacks f
                            LEFT JOIN Colaboradores c ON f.UsuarioCPF = c.CPF
                            WHERE f.Protocolo = @Protocolo";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p = cmd.CreateParameter(); p.ParameterName = "@Protocolo"; p.Value = protocolo; cmd.Parameters.Add(p);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            feedback = new Feedback
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Protocolo = reader["Protocolo"].ToString(),
                                Assunto = reader["Assunto"].ToString(),
                                Mensagem = reader["Mensagem"].ToString(),
                                DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                UsuarioCPF = reader["UsuarioCPF"] != DBNull.Value ? reader["UsuarioCPF"].ToString() : null,
                                Status = reader["Status"].ToString(),
                                AutorNome = reader["AutorNome"] != DBNull.Value ? reader["AutorNome"].ToString() : "Anônimo"
                            };
                        }
                    }
                }

                if (feedback != null)
                {
                    var sqlConversas = "SELECT * FROM FeedbackConversas WHERE FeedbackID = @ID ORDER BY DataCriacao";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlConversas;
                        var p = cmd.CreateParameter(); p.ParameterName = "@ID"; p.Value = feedback.ID; cmd.Parameters.Add(p);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                feedback.Conversas.Add(new FeedbackConversa
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    FeedbackID = Convert.ToInt32(reader["FeedbackID"]),
                                    Remetente = reader["Remetente"].ToString(),
                                    Mensagem = reader["Mensagem"].ToString(),
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    UsuarioCPF = reader["UsuarioCPF"] != DBNull.Value ? reader["UsuarioCPF"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }

            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Protocolo não encontrado.";
                return RedirectToAction(nameof(Acompanhar));
            }

            return View("Details", feedback);
        }

        [Authorize(Roles = "Admin,Diretoria/RH")]
        public IActionResult Details(int id)
        {
            Feedback feedback = null;
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var sql = @"SELECT f.*, c.Nome as AutorNome
                            FROM Feedbacks f
                            LEFT JOIN Colaboradores c ON f.UsuarioCPF = c.CPF
                            WHERE f.ID = @ID";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p = cmd.CreateParameter(); p.ParameterName = "@ID"; p.Value = id; cmd.Parameters.Add(p);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            feedback = new Feedback
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Assunto = reader["Assunto"].ToString(),
                                Mensagem = reader["Mensagem"].ToString(),
                                DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                UsuarioCPF = reader["UsuarioCPF"] != DBNull.Value ? reader["UsuarioCPF"].ToString() : null,
                                Status = reader["Status"].ToString(),
                                AutorNome = reader["AutorNome"] != DBNull.Value ? reader["AutorNome"].ToString() : "Anônimo"
                            };
                        }
                    }
                }

                if (feedback != null)
                {
                    var sqlConversas = "SELECT * FROM FeedbackConversas WHERE FeedbackID = @ID ORDER BY DataCriacao";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlConversas;
                        var p = cmd.CreateParameter(); p.ParameterName = "@ID"; p.Value = id; cmd.Parameters.Add(p);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                feedback.Conversas.Add(new FeedbackConversa
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    FeedbackID = Convert.ToInt32(reader["FeedbackID"]),
                                    Remetente = reader["Remetente"].ToString(),
                                    Mensagem = reader["Mensagem"].ToString(),
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    UsuarioCPF = reader["UsuarioCPF"] != DBNull.Value ? reader["UsuarioCPF"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }

            if (feedback == null) return NotFound();

            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult SendMessage(int feedbackId, string mensagem, string remetente = null)
        {
            if (string.IsNullOrWhiteSpace(mensagem)) return RedirectToAction(nameof(Details), new { id = feedbackId });

            string finalRemetente = remetente;
            if (string.IsNullOrEmpty(finalRemetente))
            {
                 finalRemetente = User.Identity?.IsAuthenticated == true ? (User.Identity.Name ?? "Admin") : "Autor Anônimo";
            }

            string protocolo = "";

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    // Pega o protocolo para redirecionamento caso seja anônimo
                    var sqlProto = "SELECT Protocolo FROM Feedbacks WHERE ID = @ID";
                    using (var cmdProto = connection.CreateCommand())
                    {
                        cmdProto.CommandText = sqlProto;
                        var pProto = cmdProto.CreateParameter(); pProto.ParameterName = "@ID"; pProto.Value = feedbackId; cmdProto.Parameters.Add(pProto);
                        protocolo = cmdProto.ExecuteScalar()?.ToString();
                    }

                    var sql = @"INSERT INTO FeedbackConversas (FeedbackID, UsuarioCPF, Remetente, Mensagem, DataCriacao)
                                VALUES (@FeedbackID, @UsuarioCPF, @Remetente, @Mensagem, @DataCriacao)";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@FeedbackID"; p1.Value = feedbackId; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@UsuarioCPF"; p2.Value = User.FindFirstValue("ColaboradorCPF") ?? (object)DBNull.Value; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Remetente"; p3.Value = finalRemetente; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@Mensagem"; p4.Value = mensagem; cmd.Parameters.Add(p4);
                        var p5 = cmd.CreateParameter(); p5.ParameterName = "@DataCriacao"; p5.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p5);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar resposta ao feedback.");
            }

            if (!User.Identity.IsAuthenticated || remetente == "Autor")
            {
                return RedirectToAction(nameof(Acompanhar), new { protocolo = protocolo });
            }

            return RedirectToAction(nameof(Details), new { id = feedbackId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Diretoria/RH")]
        public IActionResult Close(int id)
        {
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    var sql = "UPDATE Feedbacks SET Status = 'Fechado' WHERE ID = @ID";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p = cmd.CreateParameter(); p.ParameterName = "@ID"; p.Value = id; cmd.Parameters.Add(p);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Feedback encerrado com sucesso!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar feedback.");
                TempData["ErrorMessage"] = "Erro ao fechar feedback.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
