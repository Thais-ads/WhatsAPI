using System.ComponentModel.DataAnnotations;

namespace MagicTeste.Tabela
{
    public class TabelaWebHookJson
    {
        [Key]
        public int ID { get; set; }
        public DateTime Data { get; set; }
        public string Retorno { get; set; }
    }
}
