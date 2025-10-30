namespace ZapScrap.API.Modelos
{
    public class Imovel
    {
        public string Titulo { get; set; }
        public string Preco { get; set; }
        public string PrecoCondominio { get; set; }
        public string Link { get; set; }
        public List<string> Imagens { get; set; }
        public string Endereco { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string AreaTotal { get; set; }
        public string AreaUtil { get; set; }
        public int Quartos { get; set; }
        public int Banheiros { get; set; }
        public int Vagas { get; set; }
        public string Descricao { get; set; }
        public string TipoImovel { get; set; }
        public string Iptu { get; set; }

        public Imovel()
        {
            Imagens = new List<string>();
        }
    }
}
