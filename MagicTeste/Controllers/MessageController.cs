using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Concurrent;
using MagicTeste.Context;
using MagicTeste.Tabela;
namespace MagicTeste.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        static ConcurrentDictionary<string, string> userStates = new ConcurrentDictionary<string, string>();
        private readonly AppDbContext _dbContext;

     
        private readonly string _graphApiToken = "";

        public MessageController(IConfiguration configuration)
        {
            
           
            _dbContext = new AppDbContext(configuration);
        }
        [HttpGet("webhook")]
        public ActionResult<string> SetupWebHook([FromQuery(Name = "hub.mode")] string hubMode, [FromQuery(Name = "hub.challenge")] string hubChallenge, [FromQuery(Name = "hub.verify_token")] string hubVerifyToken)
        {
            var verifyToken = "";

            if (hubMode == "subscribe" && hubVerifyToken == verifyToken)
            {
                return Ok(hubChallenge);
            }

            return Unauthorized();
        }






        [HttpPost("webhook")]
        public async Task<IActionResult> ReceberMensagem([FromBody] dynamic item)
        {

            item = JsonConvert.DeserializeObject<dynamic>(item.ToString());
            if (item != null)
            {

                string serializedRoot = JsonConvert.SerializeObject(item);


                var value = item?.entry?[0]?.changes?[0]?.value;
                bool hasContacts = value?.contacts != null;
                bool hasStatuses = value?.statuses != null;




                if (hasContacts || value?.messages != null)
                {
                    var retornowhats = new TabelaWebHookJson
                    {
                        Retorno = serializedRoot,
                        Data = DateTime.Now
                    };

                    _dbContext.TabelaWebHookJson.Add(retornowhats);
                    await _dbContext.SaveChangesAsync();
                    await ChatBot(serializedRoot, hasContacts ? "contacts" : "messages");
                    return Ok();
                }
                else if (hasStatuses)
                {
                    await ChatBot(serializedRoot, "statuses");
                }
                else
                {
                    await ChatBot(serializedRoot, "unknown");
                }



            }

            return BadRequest();
        }



        [HttpPost("ChatBot")]
        public async Task<IActionResult> ChatBot(string json, string type)
        {
            var jsonObjects = JsonConvert.DeserializeObject<dynamic>(json);
            var message = jsonObjects?.entry?[0]?.changes?[0]?.value?.messages?[0];
            string resposta;

            if (message != null && message.type == "text")
            {
                string body = jsonObjects.entry[0].changes[0].value.messages[0].text.body;
                if (!string.IsNullOrEmpty(body))
                {
                    body = body.ToLower();
                    var cumprimentos = new[] { "oi", "ola", "bom dia", "boa tarde", "boa noite" };
                    string recipientId = jsonObjects?.entry?[0]?.changes?[0]?.value?.messages?[0]?.from;


                    if (userStates.TryGetValue(recipientId, out string currentState))
                    {

                        switch (currentState)
                        {
                            case "aguardando_nome":

                                resposta = "Obrigado! Agora, por favor, digite o CNPJ:";
                                userStates[recipientId] = "aguardando_cnpj";
                                RespostaclientCase(resposta, recipientId);
                                break;

                            case "aguardando_cnpj":

                                resposta = "CNPJ recebido! Por favor, digite um telefone para contato:";
                                userStates[recipientId] = "aguardando_telefone";
                                RespostaclientCase(resposta, recipientId);
                                break;

                            case "aguardando_telefone":

                                resposta = "Telefone recebido! Agora, descreva o tipo de chatbot necessário:";
                                userStates[recipientId] = "aguardando_descricao";
                                RespostaclientCase(resposta, recipientId);
                                break;

                            case "aguardando_descricao":

                                resposta = "Descrição recebida! Obrigado pelo seu orçamento. Entraremos em contato em breve.";
                                userStates.TryRemove(recipientId, out _);
                                RespostaclientCase(resposta, recipientId);
                                break;

                            default:

                                resposta = "Não reconhecemos sua solicitação. Por favor, escolha um tópico válido.";
                                RespostaclientCase(resposta, recipientId);
                                break;
                        }
                    }
                    else
                    {

                        if (cumprimentos.Any(c => body.Contains(c)))
                        {
                            resposta = "Seja bem vindo(a), Oi sou a Assistente Virtual Reyna: ?? \n\n" +
                                "Escolha abaixo as opções\n\n" +
                                "0. Falar com os Desenvolvedores\n" +
                                "1. Quem tem direito ao Sistema\n" +
                                "2. Requisitos obrigatórios para se usar o Chatbot\n" +
                                "3. Faça um Orçamento\n" +
                                "4. O que Oferecemos\n" +
                                "5. Sair\n";

                            bool salvabanco = await EnviarRespostaAoCliente(jsonObjects, resposta);
                        }
                        else
                        {
                            string mensagem = SegundResposta(body, recipientId);

                            switch (mensagem)
                            {
                                case "0":
                                    resposta = "Resposta selecionada 0 :\n\n Entre em contato com o numero  atravez do whatszap";
                                    RespostaclientCase(resposta, recipientId);

                                    break;
                                case "1":
                                    resposta = "Resposta selecionada 1:\n\n Quem tem direito a usar esse tipo de sistema:\n  Grande e média empresa, MEI.\n";
                                    RespostaclientCase(resposta, recipientId);

                                    break;

                                case "2":
                                    resposta = "Resposta selecionada 2:\n\n Requisitos obrigatórios para se usar o chatbot via WhatsApp.\n * ter um CNPJ válido\n  * Ter um número de telefone nunca antes vinculado ao WhatsApp Oficial, apenas para fins comerciais\n ";

                                    RespostaclientCase(resposta, recipientId);
                                    break;


                                case "3":
                                    resposta = "Resposta selecionada 3:\n\nFaça um orçamento: Preencha as informações abaixo:\n  •• Digite o Nome:";
                                    userStates[recipientId] = "aguardando_nome";
                                    RespostaclientCase(resposta, recipientId);
                                    break;
                                case "4":
                                    resposta = "Resposta selecionada 4:\n\n O que oferecemos\n •• Desenvolvimento e alinhamento do projeto junto ao cliente\n •• Atendimento e experiência do usuário\n •• Qualidade e produtividades nas demandas do dia a dia.\n •• Suporte no horário comercial\n";


                                    RespostaclientCase(resposta, recipientId);
                                    break;

                                case "5":
                                    resposta = "Resposta selecionada 5:Sair";




                                    RespostaclientCase(resposta, recipientId);
                                    break;

                                    var defaultResposta = "Não reconhecemos sua solicitação. Por favor, escolha um tópico válido.";
                                    RespostaclientCase(defaultResposta, recipientId);
                                    break;
                            }
                        }
                    }
                }
            }

            return Ok();
        }




        [HttpPost("Respostaclient")]
        public async Task<bool> EnviarRespostaAoCliente(dynamic jsonObject, string resposta)
        {
            try
            {

                string recipientId = jsonObject?.entry?[0]?.changes?[0]?.value?.messages?[0]?.from;


                if (!string.IsNullOrEmpty(recipientId))
                {

                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipientId,
                        type = "text",
                        text = new { body = resposta }
                    };

                    var jsonPayload = JsonConvert.SerializeObject(payload);


                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _graphApiToken);

                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");


                        var response = await client.PostAsync("https://graph.facebook.com/v20.0/390225890844265/messages", content);


                        return response.IsSuccessStatusCode;
                    }
                }
                else
                {

                    return false;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Erro ao enviar resposta: {ex.Message}");


                return false;
            }
        }



        [HttpPost("RespostaclientCase")]
        public async Task<bool> RespostaclientCase(dynamic resposta, string numero)
        {
            try
            {

                if (!string.IsNullOrEmpty(numero))
                {

                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        to = numero,
                        type = "text",
                        text = new { body = resposta }
                    };

                    var jsonPayload = JsonConvert.SerializeObject(payload);


                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _graphApiToken);

                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");


                        var response = await client.PostAsync("https://graph.facebook.com/v20.0/390225890844265/messages", content);


                        return response.IsSuccessStatusCode;
                    }
                }
                else
                {

                    return false;
                }
            }
            catch (Exception ex)
            {


                return false;
            }
        }



        [HttpPost("Retorno")]
        public string SegundResposta(string body, string recipientId)
        {
            var permissoes = _dbContext.TabelaWebHookJson.ToList();
            if (permissoes != null)
            {

                foreach (var permissao in permissoes)
                {
                    string retorno = permissao.Retorno;
                    var jsonObject = JsonConvert.DeserializeObject<dynamic>(retorno);
                    var firstMessage = jsonObject.entry[0].changes[0].value.messages[0].text.body.ToString();
                    var number = jsonObject.entry[0].changes[0].value.messages[0].from.ToString();

                    if (firstMessage == body && number == recipientId)
                    {
                        return firstMessage;
                    }
                }


            }

            return null;
        }





    }
}

